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
    }

    stages {
        stage('Setup Git and Authentication') {
            steps {
                script {
                    // Configure git user
                    bat '''
                        git config --global user.email "abhishekk@plasmacomp.com"
                        git config --global user.name "Abhishek-Plasma"
                    '''
                    
                    // Setup authentication - replace with your actual GitHub token
                    withCredentials([string(credentialsId: 'github-token', variable: 'GITHUB_TOKEN')]) {
                        bat '''
                            @echo off
                            echo "Setting up GitHub authentication..."
                            git remote set-url origin https://%GITHUB_TOKEN%@github.com/Abhishek-Plasma/Demo-Project.git
                        '''
                    }
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
                    echo "=== CHECKING BREAKING CHANGES ==="
                    
                    // Check if baseline exists
                    bat '''
                        @echo off
                        if not exist "SwaggerJsonGen\\swagger.json" (
                            echo "No baseline found. Creating initial baseline..."
                            copy generated-swagger.json SwaggerJsonGen\\swagger.json
                            git add SwaggerJsonGen\\swagger.json
                            git commit -m "Initial API contract"
                            echo "Initial baseline created (locally)"
                        )
                    '''
                    
                    // Compare files
                    def compareResult = bat(
                        script: 'fc "SwaggerJsonGen\\swagger.json" generated-swagger.json > diff.txt',
                        returnStatus: true
                    )
                    
                    if (compareResult == 1) {
                        echo "Breaking changes detected!"
                        
                        if (params.AUTO_COMMIT) {
                            echo "Auto-commit enabled - updating swagger.json..."
                            
                            // First, ensure we're on main branch (not detached HEAD)
                            bat '''
                                @echo off
                                echo "Checking current branch..."
                                git checkout main 2>nul || git checkout -b main
                            '''
                            
                            // Update the file
                            bat '''
                                @echo off
                                echo "Updating SwaggerJsonGen\\swagger.json..."
                                copy generated-swagger.json SwaggerJsonGen\\swagger.json
                            '''
                            
                            // Commit changes
                            bat '''
                                @echo off
                                echo "Committing changes..."
                                git add SwaggerJsonGen\\swagger.json
                                git commit -m "Update API contract - Build #%BUILD_NUMBER%"
                            '''
                            
                            // Push with explicit authentication
                            script {
                                withCredentials([string(credentialsId: 'github-token', variable: 'GITHUB_TOKEN')]) {
                                    bat '''
                                        @echo off
                                        echo "Pushing to GitHub..."
                                        git push https://%GITHUB_TOKEN%@github.com/Abhishek-Plasma/Demo-Project.git HEAD:main
                                        echo "✅ Changes pushed successfully"
                                    '''
                                }
                            }
                            
                            echo "✅ Changes committed and pushed to repository"
                        } else {
                            echo "Auto-commit disabled - failing pipeline"
                            error("Breaking changes detected. Enable AUTO_COMMIT parameter to auto-update.")
                        }
                    } else {
                        echo "✅ No breaking changes detected"
                        bat 'if exist diff.txt del diff.txt'
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
            echo '✅ Build succeeded'
        }
        failure {
            echo '❌ Build failed'
        }
    }
}
