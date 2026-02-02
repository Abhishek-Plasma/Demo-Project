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
        SWAGGER_BASELINE = 'SwaggerJsonGen/swagger.json'
        SWAGGER_GENERATED = 'generated-swagger.json'
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
                            for %%I in ("SwaggerJsonGen\\swagger.json") do echo File size: %%~zI bytes
                        ) else (
                            echo "Baseline NOT found at: SwaggerJsonGen\\swagger.json"
                            echo "Checking workspace root..."
                            if exist swagger.json (
                                echo "Found swagger.json in workspace root (not project folder)"
                            ) else (
                                echo "No baseline found anywhere"
                            )
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
                        echo "Checking for baseline in project folder..."
                        if not exist "SwaggerJsonGen\\swagger.json" (
                            echo "ERROR: Baseline swagger.json not found in project folder!"
                            echo "Expected location: SwaggerJsonGen\\swagger.json"
                            echo.
                            echo "This file should be committed to git at that location."
                            echo "If this is the first run, you can:"
                            echo "1. Run with BREAKING_CHANGE_ACTION=COMMIT to create it"
                            echo "2. Or manually commit swagger.json to SwaggerJsonGen/ folder"
                            exit /b 1
                        )
                        echo "Baseline exists at SwaggerJsonGen\\swagger.json"
                    '''
                    bat '''
                        @echo off
                        echo "Comparing API definitions..."
                        echo "Baseline: SwaggerJsonGen\\swagger.json"
                        echo "Current:  generated-swagger.json"
                        echo.
                        fc "SwaggerJsonGen\\swagger.json" generated-swagger.json > diff.txt
                        if errorlevel 1 (
                            echo "BREAKING CHANGES DETECTED!"
                            echo "Differences:"
                            type diff.txt
                            echo.
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
                        echo "COMMIT action selected - updating SwaggerJsonGen/swagger.json and committing to git..."
                        bat '''
                            @echo off
                            echo "Step 1: Updating baseline with new API version..."
                            copy generated-swagger.json SwaggerJsonGen\\swagger.json
                            echo "Step 2: Configuring git user..."
                            git config user.email "jenkins@example.com"
                            git config user.name "Jenkins CI"
                            echo "Step 3: Committing changes..."
                            git add SwaggerJsonGen\\swagger.json
                            git commit -m "${params.COMMIT_MESSAGE} - Build #${env.BUILD_NUMBER}"
                            echo "Step 4: Pushing to repository..."
                            git push origin HEAD
                            echo "API contract updated and committed to: SwaggerJsonGen/swagger.json"
                        '''
                        emailext (
                            subject: "API Contract Updated - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                            body: """
                                <h2>API Contract Successfully Updated</h2>
                                <p>Breaking changes were detected and the API contract has been updated.</p>
                                <p><b>Action:</b> COMMIT</p>
                                <p><b>Job:</b> ${env.JOB_NAME}</p>
                                <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                                <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                                <p><b>Commit Message:</b> ${params.COMMIT_MESSAGE}</p>
                                <p><b>Updated File:</b> SwaggerJsonGen/swagger.json</p>
                                <h3>Changes Detected:</h3>
                                <pre>${diffContent}</pre>
                                <p>The swagger.json file has been updated and committed to the repository.</p>
                            """,
                            to: "${env.EMAIL_RECIPIENTS}",
                            mimeType: 'text/html'
                        )
                    } else if (params.BREAKING_CHANGE_ACTION == 'FAIL') {
                        echo "FAIL action selected - failing pipeline..."
                        error("""❌ BREAKING CHANGES DETECTED!
                            Baseline: SwaggerJsonGen/swagger.json
                            Current:  generated-swagger.json
                            Changes:
                            ${diffContent}
                            Action: FAIL (pipeline will fail)
                            To fix:
                            1. Review the breaking changes above
                            2. If changes are intentional, re-run with BREAKING_CHANGE_ACTION=COMMIT
                            3. If changes are not intentional, fix your API code""")
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
                echo "Workspace contents:"
                echo "Project folder (SwaggerJsonGen\\swagger.json):"
                if exist "SwaggerJsonGen\\swagger.json" (
                    for %%I in ("SwaggerJsonGen\\swagger.json") do echo   %%~zI bytes
                ) else (
                    echo "   Not found"
                )
                echo "Generated file (generated-swagger.json):"
                if exist generated-swagger.json (
                    for %%I in (generated-swagger.json) do echo   %%~zI bytes
                )
                echo "Diff file:"
                if exist diff.txt (
                    echo "   diff.txt exists"
                )
            '''
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
                            <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                            <p><b>Baseline:</b> SwaggerJsonGen/swagger.json</p>
                        """,
                        to: "${env.EMAIL_RECIPIENTS}",
                        mimeType: 'text/html'
                    )
                }
            }
        }
        failure {
            echo 'Pipeline failed - Breaking changes detected'
            script {
                if (fileExists('diff.txt')) {
                    def diffContent = readFile('diff.txt').trim()
                    emailext (
                        subject: "API Breaking Changes - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                        body: """
                            <h2>API Breaking Changes Detected</h2>
                            <p>The API validation pipeline has failed due to breaking changes.</p>
                            <p><b>Action Selected:</b> FAIL</p>
                            <p><b>Job:</b> ${env.JOB_NAME}</p>
                            <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                            <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                            <p><b>Baseline Location:</b> SwaggerJsonGen/swagger.json</p>
                            <h3>Breaking Changes:</h3>
                            <pre>${diffContent}</pre>
                            <h3>Next Steps:</h3>
                            <ol>
                                <li><b>Review the breaking changes</b> listed above</li>
                                <li><b>If changes are intentional:</b>
                                    <ul>
                                        <li>Re-run the pipeline with BREAKING_CHANGE_ACTION = COMMIT</li>
                                        <li>This will update SwaggerJsonGen/swagger.json and commit to git</li>
                                    </ul>
                                </li>
                                <li><b>If changes are not intentional:</b> Fix the API code in your project</li>
                            </ol>
                            <p><b>Note:</b> The baseline is at SwaggerJsonGen/swagger.json</p>
                        """,
                        to: "${env.EMAIL_RECIPIENTS}",
                        mimeType: 'text/html'
                    )
                }
            }
        }
    }
}
