pipeline {
    agent any

    parameters {
        booleanParam(
            name: 'AUTO_COMMIT',
            defaultValue: false,
            description: 'Check to auto-commit breaking changes'
        )
    }

    environment {
        EMAIL_RECIPIENTS = 'theak18012002@gmail.com'
        BUILD_URL = "${env.BUILD_URL}"
        JOB_NAME = "${env.JOB_NAME}"
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
        REPO_URL = 'https://github.com/Abhishek-Plasma/Demo-Project.git'
    }

    stages {
        stage('Setup Git Configuration') {
            steps {
                script {
                    bat '''
                        git config --global user.email "abhishekk@plasmacomp.com"
                        git config --global user.name "Abhishek-Plasma"
                        git config --global core.autocrlf false
                    '''
                }
            }
        }

        stage('Clean Workspace') {
            steps {
                bat '''
                    @echo off
                    echo "Cleaning workspace..."
                    if exist generated-swagger.json del generated-swagger.json
                    if exist diff.txt del diff.txt
                    if exist breaking_changes_report.txt del breaking_changes_report.txt
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
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj --configuration Release
                '''
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json..."
                    dotnet swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Release\\net8.0\\SwaggerJsonGen.dll v1
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

        stage('Check and Handle Breaking Changes') {
            steps {
                script {
                    // Check if baseline exists
                    bat '''
                        @echo off
                        if not exist "SwaggerJsonGen\\swagger.json" (
                            echo "No baseline found. Creating initial baseline..."
                            copy generated-swagger.json SwaggerJsonGen\\swagger.json
                        )
                    '''
                    
                    // Check if baseline was created in this run
                    def baselineExists = bat(
                        script: '@echo off && if exist "SwaggerJsonGen\\swagger.json" (echo exists) else (echo not_exists)',
                        returnStdout: true
                    ).trim()
                    
                    if (baselineExists == "not_exists") {
                        // Initial baseline created, we can stop here or commit
                        echo "Initial baseline created. Manual push required."
                        currentBuild.result = 'UNSTABLE'
                        return
                    }
                    
                    // Compare files - using PowerShell for better comparison
                    def compareResult = bat(
                        script: '''
                            @echo off
                            powershell -Command "if ((Get-Content 'SwaggerJsonGen\\swagger.json' -Raw) -eq (Get-Content 'generated-swagger.json' -Raw)) { exit 0 } else { exit 1 }"
                        ''',
                        returnStatus: true
                    )
                    
                    if (compareResult == 1) {
                        echo "Breaking changes detected!"
                        
                        // Create a diff report
                        bat '''
                            @echo off
                            echo "=== BREAKING CHANGES DETECTED ===" > breaking_changes_report.txt
                            echo "Build: %BUILD_NUMBER%" >> breaking_changes_report.txt
                            echo "Date: %DATE% %TIME%" >> breaking_changes_report.txt
                            echo. >> breaking_changes_report.txt
                            echo "Differences between baseline and generated swagger:" >> breaking_changes_report.txt
                            echo "==================================================" >> breaking_changes_report.txt
                            fc "SwaggerJsonGen\\swagger.json" "generated-swagger.json" >> breaking_changes_report.txt 2>&1
                        '''
                        
                        if (params.AUTO_COMMIT) {
                            echo "Auto-commit enabled - updating swagger.json..."
                            
                            withCredentials([usernamePassword(
                                credentialsId: 'github-token',
                                usernameVariable: 'GIT_USERNAME',
                                passwordVariable: 'GIT_TOKEN'
                            )]) {
                                // Clone repository fresh to avoid detached HEAD issues
                                bat '''
                                    @echo off
                                    echo "Setting up fresh repository clone..."
                                    
                                    REM Create a clean directory for git operations
                                    if exist "temp_repo" rd /s /q "temp_repo"
                                    mkdir temp_repo
                                    cd temp_repo
                                    
                                    REM Clone with token authentication
                                    git clone https://%GIT_USERNAME%:%GIT_TOKEN%@github.com/Abhishek-Plasma/Demo-Project.git .
                                    
                                    REM Copy the new swagger file
                                    copy "..\\generated-swagger.json" "SwaggerJsonGen\\swagger.json"
                                    
                                    REM Configure git
                                    git config user.email "abhishekk@plasmacomp.com"
                                    git config user.name "Abhishek-Plasma"
                                    
                                    REM Check if there are changes
                                    git status
                                    git diff --quiet SwaggerJsonGen\\swagger.json
                                    if errorlevel 1 (
                                        echo "Changes detected, committing..."
                                        git add SwaggerJsonGen\\swagger.json
                                        git commit -m "Update API contract - Build #%BUILD_NUMBER%"
                                        git push origin main
                                        echo "✅ Changes committed and pushed successfully"
                                    ) else (
                                        echo "No changes to commit"
                                    )
                                    
                                    cd ..
                                '''
                            }
                        } else {
                            echo "Auto-commit disabled - breaking changes detected"
                            error("Breaking changes detected. Enable AUTO_COMMIT parameter to auto-update.")
                        }
                    } else {
                        echo "✅ No breaking changes detected"
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
            
            // Clean up temp directory
            bat 'if exist temp_repo rd /s /q temp_repo'
        }
        success {
            echo '✅ Build succeeded'
        }
        failure {
            echo '❌ Build failed'
            // Optionally send email notification
        }
        unstable {
            echo '⚠️ Build unstable - initial baseline created'
        }
    }
}
