pipeline {
    agent any

    parameters {
        booleanParam(
            name: 'AUTO_COMMIT',
            defaultValue: false,
            description: 'Check to auto-commit breaking changes'
        )
        choice(
            name: 'ON_BREAKING_CHANGE',
            choices: ['FAIL', 'WARN', 'UNSTABLE'],
            description: 'What to do when breaking changes are detected'
        )
    }

    environment {
        EMAIL_RECIPIENTS = 'theak18012002@gmail.com'
        BUILD_URL = "${env.BUILD_URL}"
        JOB_NAME = "${env.JOB_NAME}"
        BUILD_NUMBER = "${env.BUILD_NUMBER}"
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
                    def baselineExists = fileExists('SwaggerJsonGen/swagger.json')
                    
                    if (!baselineExists) {
                        echo "No baseline found. Creating initial baseline..."
                        bat 'copy generated-swagger.json SwaggerJsonGen\\swagger.json'
                        echo "✅ Initial baseline created"
                        currentBuild.result = 'SUCCESS'
                        return
                    }
                    
                    // Compare files using file comparison
                    def compareResult = bat(
                        script: '''
                            @echo off
                            fc "SwaggerJsonGen\\swagger.json" generated-swagger.json > diff.txt
                            echo %errorlevel%
                        ''',
                        returnStdout: true
                    ).trim()
                    
                    if (compareResult == "0") {
                        echo "✅ No breaking changes detected"
                        bat 'if exist diff.txt del diff.txt'
                    } else {
                        echo "⚠️ Breaking changes detected!"
                        
                        // Create a detailed report
                        bat '''
                            @echo off
                            echo "=== API BREAKING CHANGES REPORT ===" > breaking_changes_report.txt
                            echo "Build Number: %BUILD_NUMBER%" >> breaking_changes_report.txt
                            echo "Job: %JOB_NAME%" >> breaking_changes_report.txt
                            echo "Date: %DATE% %TIME%" >> breaking_changes_report.txt
                            echo "=====================================" >> breaking_changes_report.txt
                            echo. >> breaking_changes_report.txt
                            echo "DIFFERENCES FOUND:" >> breaking_changes_report.txt
                            echo "==================" >> breaking_changes_report.txt
                            type diff.txt >> breaking_changes_report.txt
                            echo. >> breaking_changes_report.txt
                            echo "SUMMARY:" >> breaking_changes_report.txt
                            echo "=========" >> breaking_changes_report.txt
                            echo "- Old file: SwaggerJsonGen\\swagger.json" >> breaking_changes_report.txt
                            echo "- New file: generated-swagger.json" >> breaking_changes_report.txt
                            echo "- Auto-commit enabled: ''' + params.AUTO_COMMIT + '''" >> breaking_changes_report.txt
                        '''
                        
                        // Display diff in console
                        def diffContent = bat(
                            script: '@echo off && type diff.txt',
                            returnStdout: true
                        )
                        echo "Diff output:\n${diffContent}"
                        
                        // Handle based on parameter choice
                        if (params.AUTO_COMMIT) {
                            echo "Auto-commit enabled - attempting to update..."
                            
                            try {
                                // Ensure we're on main branch
                                bat '''
                                    @echo off
                                    echo "Checking git status..."
                                    git checkout main 2>nul || echo "Already on main or cannot checkout"
                                    git pull origin main
                                '''
                                
                                // Update the swagger file
                                bat 'copy generated-swagger.json SwaggerJsonGen\\swagger.json /Y'
                                
                                // Commit changes
                                withCredentials([usernamePassword(
                                    credentialsId: 'github-token',
                                    usernameVariable: 'GIT_USERNAME',
                                    passwordVariable: 'GIT_TOKEN'
                                )]) {
                                    bat '''
                                        @echo off
                                        git add SwaggerJsonGen\\swagger.json
                                        git commit -m "Update API contract - Build #%BUILD_NUMBER%"
                                        
                                        REM Configure remote with token
                                        git remote set-url origin https://%GIT_USERNAME%:%GIT_TOKEN%@github.com/Abhishek-Plasma/Demo-Project.git
                                        git push origin main
                                        
                                        echo "✅ Changes committed and pushed successfully"
                                    '''
                                }
                                
                                echo "✅ API contract updated and committed"
                            } catch (Exception e) {
                                echo "❌ Failed to auto-commit: ${e.message}"
                                currentBuild.result = 'FAILURE'
                                error("Auto-commit failed")
                            }
                        } else {
                            // Handle based on user choice
                            switch(params.ON_BREAKING_CHANGE) {
                                case 'FAIL':
                                    echo "❌ Failing build due to breaking changes"
                                    currentBuild.result = 'FAILURE'
                                    error("Breaking changes detected. Set AUTO_COMMIT=true to auto-update or set ON_BREAKING_CHANGE to 'WARN'.")
                                    break
                                case 'WARN':
                                    echo "⚠️ Breaking changes detected but continuing build"
                                    echo "You can view the differences in breaking_changes_report.txt"
                                    currentBuild.result = 'SUCCESS'
                                    break
                                case 'UNSTABLE':
                                    echo "⚠️ Marking build as unstable due to breaking changes"
                                    currentBuild.result = 'UNSTABLE'
                                    break
                            }
                        }
                    }
                }
            }
        }
    }

    post {
        always {
            echo "=== BUILD COMPLETED ==="
            echo "Status: ${currentBuild.currentResult}"
            
            // Archive artifacts
            archiveArtifacts artifacts: '*.json, *.txt', allowEmptyArchive: true
            
            // Cleanup
            bat '''
                @echo off
                echo "Final files in workspace:"
                dir /b *.json *.txt 2>nul || echo "No files found"
            '''
        }
        
        success {
            echo '✅ Build succeeded'
            // Optional: Send success email
        }
        
        failure {
            echo '❌ Build failed'
            // Optional: Send failure email with report
        }
        
        unstable {
            echo '⚠️ Build unstable - breaking changes detected'
            // Optional: Send unstable notification
        }
    }
}
