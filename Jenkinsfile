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
        EMAIL_RECIPIENTS = 'abhishek@example.com'  // Update with your email
        BUILD_URL = "${env.BUILD_URL}"
        JOB_NAME = "${env.JOB_NAME}"
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
        BRANCH_NAME = "${env.BRANCH_NAME ?: 'main'}"
    }

    stages {
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

        stage('Detect Breaking Changes') {
            steps {
                script {
                    echo "=== DETECTING BREAKING CHANGES ==="
                    echo "Action selected: ${params.BREAKING_CHANGE_ACTION}"
                    
                    // Check if baseline exists
                    bat '''
                        @echo off
                        echo "Checking for baseline swagger.json..."
                        if not exist swagger.json (
                            echo "No baseline found. Creating baseline..."
                            copy generated-swagger.json swagger.json
                            echo "Baseline created."
                            exit /b 0
                        )
                    '''
                    
                    // Compare files and save diff
                    bat '''
                        @echo off
                        echo "Comparing with baseline..."
                        fc swagger.json generated-swagger.json > diff.txt
                        
                        if errorlevel 1 (
                            echo "BREAKING CHANGES DETECTED"
                            exit /b 1
                        ) else (
                            echo "No breaking changes detected"
                            exit /b 0
                        )
                    '''
                }
            }
        }

        stage('Handle Breaking Changes') {
            when {
                expression { 
                    // Only run this stage if breaking changes were detected
                    // We'll check if diff.txt exists (created in previous stage)
                    fileExists('diff.txt')
                }
            }
            steps {
                script {
                    echo "=== HANDLING BREAKING CHANGES ==="
                    
                    // Read the diff for reporting
                    def diffContent = readFile('diff.txt')
                    
                    if (params.BREAKING_CHANGE_ACTION == 'COMMIT') {
                        echo "Action: COMMIT - Will update swagger.json and commit to git"
                        
                        // Update swagger.json with new version
                        bat '''
                            @echo off
                            echo "Updating swagger.json with new API version..."
                            copy generated-swagger.json swagger.json
                            
                            echo "Configuring git user..."
                            git config user.email "jenkins@example.com"
                            git config user.name "Jenkins CI"
                            
                            echo "Committing changes..."
                            git add swagger.json
                            git commit -m "${params.COMMIT_MESSAGE} - Build #${env.BUILD_NUMBER}"
                            
                            echo "Pushing to repository..."
                            git push origin ${env.BRANCH_NAME}
                            
                            echo "✅ API contract updated and committed"
                        '''
                        
                        // Send success email
                        emailext (
                            subject: "✅ API Contract Updated - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                            body: """
                                <h2>API Contract Successfully Updated</h2>
                                <p>Breaking changes were detected and the API contract has been updated.</p>
                                
                                <p><b>Action:</b> COMMIT</p>
                                <p><b>Job:</b> ${env.JOB_NAME}</p>
                                <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                                <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                                <p><b>Branch:</b> ${env.BRANCH_NAME}</p>
                                <p><b>Commit Message:</b> ${params.COMMIT_MESSAGE}</p>
                                
                                <h3>Changes Detected:</h3>
                                <pre>${diffContent}</pre>
                                
                                <p>The swagger.json file has been updated and committed to the repository.</p>
                            """,
                            to: "${env.EMAIL_RECIPIENTS}",
                            mimeType: 'text/html'
                        )
                        
                    } else if (params.BREAKING_CHANGE_ACTION == 'FAIL') {
                        echo "Action: FAIL - Failing pipeline due to breaking changes"
                        
                        // Fail the build
                        error("❌ Breaking changes detected. Pipeline failed as per BREAKING_CHANGE_ACTION=FAIL")
                    }
                }
            }
        }
    }

    post {
        always {
            echo "=== BUILD COMPLETED ==="
            echo "Status: ${currentBuild.currentResult}"
            
            bat '''
                @echo off
                echo "Files in workspace:"
                dir /b *.json *.txt 2>nul || echo "No files found"
            '''
            
            archiveArtifacts artifacts: '*.json, *.txt', allowEmptyArchive: true
        }
        
        success {
            // This runs when build succeeds (no breaking changes or they were committed)
            echo '✅ Pipeline completed successfully'
            
            // Only send success email if no breaking changes were handled
            script {
                if (!fileExists('diff.txt')) {
                    emailext (
                        subject: "✅ API Validation Passed - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                        body: """
                            <h2>API Validation Successful</h2>
                            <p>No breaking changes detected in the API.</p>
                            
                            <p><b>Job:</b> ${env.JOB_NAME}</p>
                            <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                            <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                            <p><b>Branch:</b> ${env.BRANCH_NAME}</p>
                        """,
                        to: "${env.EMAIL_RECIPIENTS}",
                        mimeType: 'text/html'
                    )
                }
            }
        }
        
        failure {
            // This runs when build fails (breaking changes with FAIL action)
            echo '❌ Pipeline failed'
            
            script {
                if (fileExists('diff.txt')) {
                    def diffContent = readFile('diff.txt')
                    
                    emailext (
                        subject: "❌ API Breaking Changes - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                        body: """
                            <h2>🚨 API Breaking Changes Detected</h2>
                            <p>The API validation pipeline has failed due to breaking changes.</p>
                            
                            <p><b>Action Selected:</b> FAIL</p>
                            <p><b>Job:</b> ${env.JOB_NAME}</p>
                            <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                            <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                            <p><b>Branch:</b> ${env.BRANCH_NAME}</p>
                            
                            <h3>Breaking Changes:</h3>
                            <pre>${diffContent}</pre>
                            
                            <h3>Next Steps:</h3>
                            <ol>
                                <li><b>Review the breaking changes</b> listed above</li>
                                <li><b>If changes are intentional:</b>
                                    <ul>
                                        <li>Re-run the pipeline with <code>BREAKING_CHANGE_ACTION = COMMIT</code></li>
                                        <li>Or manually update: <code>copy generated-swagger.json swagger.json</code></li>
                                        <li>Commit to git: <code>git add swagger.json && git commit -m "Update API contract" && git push</code></li>
                                    </ul>
                                </li>
                                <li><b>If changes are not intentional:</b> Fix the API code</li>
                            </ol>
                        """,
                        to: "${env.EMAIL_RECIPIENTS}",
                        mimeType: 'text/html'
                    )
                } else {
                    // Generic failure email for other errors
                    emailext (
                        subject: "❌ Pipeline Failed - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                        body: """
                            <h2>Pipeline Failed</h2>
                            <p>The API validation pipeline has failed.</p>
                            
                            <p><b>Job:</b> ${env.JOB_NAME}</p>
                            <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                            <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                            <p><b>Branch:</b> ${env.BRANCH_NAME}</p>
                            
                            <p>Check the build logs for details.</p>
                        """,
                        to: "${env.EMAIL_RECIPIENTS}",
                        mimeType: 'text/html'
                    )
                }
            }
        }
    }
}
