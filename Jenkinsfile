pipeline {
    agent any

    parameters {
        choice(
            name: 'BREAKING_CHANGE_ACTION',
            choices: ['FAIL', 'COMMIT'],
            description: 'FAIL: Fail pipeline if breaking changes found. COMMIT: Commit updated swagger.json to git if breaking changes found.'
        )
        string(
            name: 'COMMIT_MESSAGE',
            defaultValue: 'Update API contract',
            description: 'Commit message for updating swagger.json'
        )
    }

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    environment {
        EMAIL_RECIPIENTS = 'theak18012002@gmail.com'
        BUILD_URL = "${env.BUILD_URL}"
        JOB_NAME = "${env.JOB_NAME}"
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
        GIT_AUTHOR_EMAIL = 'jenkins@example.com'
        GIT_AUTHOR_NAME = 'Jenkins CI'
    }

    stages {
        stage('Setup Git Configuration') {
            steps {
                bat '''
                    @echo off
                    echo "Configuring git user..."
                    git config --global user.email "jenkins@example.com"
                    git config --global user.name "Jenkins CI"
                    git config --global push.default simple
                '''
            }
        }

        stage('Clean Workspace') {
            steps {
                bat '''
                    @echo off
                    echo "Cleaning workspace..."
                    if exist generated-swagger.json del generated-swagger.json
                    if exist diff.txt del diff.txt
                '''
            }
        }

        stage('Install Tools') {
            steps {
                bat '''
                    @echo off
                    echo "Installing Swashbuckle CLI..."
                    dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                '''
            }
        }

        stage('Build') {
            steps {
                bat '''
                    @echo off
                    echo "Building project..."
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj
                '''
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json..."
                    dotnet swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    if exist generated-swagger.json (
                        echo "SUCCESS: Swagger file generated"
                        for %%I in (generated-swagger.json) do echo File size: %%~zI bytes
                    ) else (
                        echo "ERROR: Failed to generate swagger"
                        exit /b 1
                    )
                '''
            }
        }

        stage('Check Baseline Location') {
            steps {
                script {
                    echo "=== CHECKING BASELINE LOCATION ==="
                    bat '''
                        @echo off
                        echo "Looking for baseline swagger.json..."
                        echo "Checking: SwaggerJsonGen\\swagger.json"
                        if exist "SwaggerJsonGen\\swagger.json" (
                            echo "Found baseline at: SwaggerJsonGen\\swagger.json"
                        ) else (
                            echo "Baseline NOT found at: SwaggerJsonGen\\swagger.json"
                        )
                    '''
                }
            }
        }

        stage('Detect Breaking Changes') {
            steps {
                script {
                    echo "=== DETECTING BREAKING CHANGES ==="
                    echo "Action selected: ${params.BREAKING_CHANGE_ACTION}"
                    
                    bat '''
                        @echo off
                        echo "Checking for baseline..."
                        if not exist "SwaggerJsonGen\\swagger.json" (
                            echo "ERROR: Baseline swagger.json not found!"
                            echo "Expected location: SwaggerJsonGen\\swagger.json"
                            exit /b 1
                        )
                        
                        echo "Comparing API definitions..."
                        fc "SwaggerJsonGen\\swagger.json" generated-swagger.json > diff.txt
                        
                        if errorlevel 1 (
                            echo "BREAKING CHANGES DETECTED!"
                            exit /b 0
                        ) else (
                            echo "No breaking changes detected"
                            if exist diff.txt del diff.txt
                        )
                    '''
                }
            }
        }

        stage('Handle Breaking Changes') {
            when {
                expression { 
                    fileExists('diff.txt')
                }
            }
            steps {
                script {
                    echo "=== HANDLING BREAKING CHANGES ==="
                    def diffContent = readFile('diff.txt').trim()
                    
                    if (params.BREAKING_CHANGE_ACTION == 'COMMIT') {
                        echo "COMMIT action selected - updating swagger.json..."
                        
                        // First, check if we're on a branch or detached HEAD
                        bat '''
                            @echo off
                            echo "Checking git status..."
                            git status
                            git checkout main
                            git pull origin main
                        '''
                        
                        // Update the file
                        bat '''
                            @echo off
                            echo "Updating SwaggerJsonGen\\swagger.json..."
                            copy generated-swagger.json SwaggerJsonGen\\swagger.json
                        '''
                        
                        // Pull latest changes to avoid conflicts
                        bat '''
                            @echo off
                            echo "Pulling latest changes..."
                            git pull origin main --no-rebase
                        '''
                        
                        // Commit and push
                        bat '''
                            @echo off
                            echo "Committing and pushing changes..."
                            git add SwaggerJsonGen\\swagger.json
                            git commit -m "%COMMIT_MESSAGE% - Build #%BUILD_NUMBER%"
                            git push
                            echo "✅ Changes committed and pushed to repository"
                        '''
                        
                        // Send success email
                        emailext (
                            subject: "API Contract Updated - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                            body: """
                                <h2>API Contract Successfully Updated</h2>
                                <p>Breaking changes were detected and the API contract has been updated.</p>
                                <p><b>Job:</b> ${env.JOB_NAME}</p>
                                <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                                <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                                <p><b>Commit Message:</b> ${params.COMMIT_MESSAGE}</p>
                                <h3>Changes Detected:</h3>
                                <pre>${diffContent}</pre>
                            """,
                            to: "${env.EMAIL_RECIPIENTS}",
                            mimeType: 'text/html'
                        )
                        
                    } else if (params.BREAKING_CHANGE_ACTION == 'FAIL') {
                        echo "FAIL action selected - failing pipeline..."
                        error("❌ Breaking changes detected. Re-run with COMMIT action to update.")
                    }
                }
            }
        }
    }

    post {
        always {
            echo "=== BUILD COMPLETED ==="
            echo "Status: ${currentBuild.currentResult}"
            archiveArtifacts artifacts: '*.json, *.txt', allowEmptyArchive: true
        }
        success {
            echo 'Pipeline completed successfully'
            script {
                if (!fileExists('diff.txt')) {
                    emailext (
                        subject: "API Validation Passed - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                        body: """
                            <h2>API Validation Successful</h2>
                            <p>No breaking changes detected in the API.</p>
                            <p><b>Job:</b> ${env.JOB_NAME}</p>
                            <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                        """,
                        to: "${env.EMAIL_RECIPIENTS}",
                        mimeType: 'text/html'
                    )
                }
            }
        }
        failure {
            echo 'Pipeline failed'
        }
    }
}
