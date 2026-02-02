pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    environment {
        EMAIL_RECIPIENTS = 'abhishek@example.com'
        BUILD_URL = "${env.BUILD_URL}"
        JOB_NAME = "${env.JOB_NAME}"
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
    }

    stages {
        stage('Clean Workspace') {
            steps {
                bat '''
                    @echo off
                    echo "Cleaning up..."
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

        stage('Check Breaking Changes') {
            steps {
                script {
                    echo "=== CHECKING FOR BREAKING CHANGES ==="
                    
                    bat '''
                        @echo off
                        echo "Step 1: Verify files exist..."
                        if not exist generated-swagger.json (
                            echo "ERROR: No generated-swagger.json found"
                            exit /b 1
                        )
                        
                        if not exist swagger.json (
                            echo "INFO: No baseline found. Creating baseline..."
                            copy generated-swagger.json swagger.json
                            echo "Baseline created from current version."
                            exit /b 0
                        )
                        
                        echo "Step 2: Compare files..."
                        fc swagger.json generated-swagger.json > diff.txt
                        
                        if errorlevel 1 (
                            echo "❌ BREAKING CHANGES DETECTED!"
                            echo.
                            echo "=== REQUIRED ACTION ==="
                            echo "Your API has breaking changes. To proceed:"
                            echo.
                            echo "1. Review the differences:"
                            type diff.txt
                            echo.
                            echo "2. If changes are intentional, update swagger.json:"
                            echo "   copy generated-swagger.json swagger.json"
                            echo.
                            echo "3. Commit the updated swagger.json to git:"
                            echo "   git add swagger.json"
                            echo "   git commit -m 'Update API contract'"
                            echo "   git push"
                            echo.
                            echo "4. Re-run this pipeline"
                            echo.
                            exit /b 1
                        ) else (
                            echo "✅ No breaking changes detected"
                            if exist diff.txt del diff.txt
                        )
                    '''
                }
            }
        }
        
        stage('Validate swagger.json is committed') {
            when {
                expression { currentBuild.result == 'SUCCESS' }
            }
            steps {
                script {
                    // This stage ensures swagger.json is committed to git
                    echo "Validating that swagger.json is in sync with git..."
                    
                    // Check if swagger.json has uncommitted changes
                    bat '''
                        @echo off
                        echo "Checking git status of swagger.json..."
                        git status --porcelain swagger.json 2>nul
                        if errorlevel 0 (
                            echo "WARNING: swagger.json has uncommitted changes"
                            echo "Please commit swagger.json to git:"
                            echo "  git add swagger.json"
                            echo "  git commit -m 'Update API contract'"
                            echo "  git push"
                        ) else (
                            echo "✅ swagger.json is committed to git"
                        )
                    '''
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
        failure {
            script {
                def breakingChanges = readFile('diff.txt').trim()
                
                emailext (
                    subject: "❌ API Breaking Changes - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                    body: """
                        <h2>🚨 API Breaking Changes Detected</h2>
                        <p>The API validation pipeline has failed due to breaking changes.</p>
                        
                        <p><b>Job:</b> ${env.JOB_NAME}</p>
                        <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                        <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                        
                        <h3>Required Action:</h3>
                        <p>To proceed, you must commit the updated API contract to git:</p>
                        
                        <ol>
                            <li><b>Review the breaking changes:</b>
                                <pre>${breakingChanges}</pre>
                            </li>
                            <li><b>Update the baseline:</b><br>
                                <code>copy generated-swagger.json swagger.json</code>
                            </li>
                            <li><b>Commit to git:</b><br>
                                <code>git add swagger.json</code><br>
                                <code>git commit -m "Update API contract"</code><br>
                                <code>git push</code>
                            </li>
                            <li><b>Re-run the pipeline</b></li>
                        </ol>
                        
                        <p>The generated API specification is available in the build artifacts.</p>
                    """,
                    to: "${env.EMAIL_RECIPIENTS}",
                    mimeType: 'text/html'
                )
            }
        }
    }
}
