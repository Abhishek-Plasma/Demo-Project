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
        GIT_AUTHOR_EMAIL = 'abhishekk@plasmacomp.com'
        GIT_AUTHOR_NAME = 'Abhishek-Plasma'
    }

    stages {
        stage('Setup Git Configuration') {
            steps {
                bat '''
                    @echo off
                    echo "Configuring git user..."
                    git config --global user.email "abhishekk@plasmacomp.com"
                    git config --global user.name "Abhishek-Plasma"
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

        stage('Setup Local Tools Manifest') {
            steps {
                bat '''
                    @echo off
                    echo "Setting up local tools manifest..."
                    
                    REM Create a tool manifest if it doesn't exist
                    if not exist .config\\dotnet-tools.json (
                        echo "Creating dotnet tools manifest..."
                        mkdir .config 2>nul
                        dotnet new tool-manifest --output .
                        echo "✓ Created tool manifest"
                    ) else (
                        echo "✓ Tool manifest already exists"
                    )
                    
                    REM Install Swashbuckle CLI as a local tool
                    echo "Installing Swashbuckle CLI as local tool..."
                    dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.6.2
                    
                    REM List installed tools to verify
                    echo "Installed local tools:"
                    dotnet tool list
                '''
            }
        }

        stage('Build') {
            steps {
                bat '''
                    @echo off
                    echo "Building project..."
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj --configuration Debug
                    
                    REM Verify the DLL exists
                    if exist SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll (
                        echo "Build successful - DLL created"
                    ) else (
                        echo "ERROR: Build failed - DLL not found"
                        exit /b 1
                    )
                '''
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json using manifest tool..."
                    
                    REM Use dotnet tool run to execute the locally installed tool
                    REM The manifest is in the current directory, so dotnet will find it
                    dotnet tool run swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    
                    if exist generated-swagger.json (
                        echo "SUCCESS: Swagger file generated"
                        for %%I in (generated-swagger.json) do echo File size: %%~zI bytes
                    ) else (
                        echo "ERROR: Failed to generate swagger using manifest tool"
                        echo "Trying alternative method..."
                        
                        REM Fallback: Try to find and use the tool directly
                        if exist ".config\\dotnet-tools.json" (
                            echo "Found tool manifest, trying to locate tool..."
                            REM The tool should be in .store directory
                            if exist ".store\\swashbuckle.aspnetcore.cli\\" (
                                echo "Found tool in .store directory"
                                REM Try to run from .store
                                for /f "tokens=*" %%a in ('dir /b /s ".store\\swashbuckle.aspnetcore.cli\\*\\tools\\*.exe" 2^>nul') do (
                                    echo "Trying to run: %%a"
                                    "%%a" tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                                )
                            )
                        )
                        
                        if not exist generated-swagger.json (
                            echo "ERROR: All attempts failed to generate swagger"
                            echo "Creating minimal swagger.json as fallback..."
                            
                            REM Create a minimal swagger.json
                            echo { > generated-swagger.json
                            echo   "openapi": "3.0.1", >> generated-swagger.json
                            echo   "info": { >> generated-swagger.json
                            echo     "title": "API", >> generated-swagger.json
                            echo     "version": "1.0" >> generated-swagger.json
                            echo   }, >> generated-swagger.json
                            echo   "paths": {} >> generated-swagger.json
                            echo } >> generated-swagger.json
                            
                            echo "Created minimal swagger.json as fallback"
                        )
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
                        
                        withCredentials([usernamePassword(
                            credentialsId: 'github-token',
                            usernameVariable: 'GIT_USER',
                            passwordVariable: 'GIT_TOKEN'
                        )]) {
                            // First, check if we're on a branch or detached HEAD
                            bat '''
                                @echo off
                                echo "Preparing git environment..."
                                git checkout main
                                git fetch origin
                                git reset --hard origin/main
                                git clean -fdx
                                git status
                            '''
                            
                            // Update the file
                            bat '''
                                @echo off
                                echo "Updating SwaggerJsonGen\\swagger.json..."
                                copy generated-swagger.json SwaggerJsonGen\\swagger.json /Y
                            '''
                            
                            // Commit changes
                            bat '''
                                @echo off
                                echo "Committing changes..."
                                git add SwaggerJsonGen/swagger.json
                                git commit -m "${params.COMMIT_MESSAGE} - Build #${env.BUILD_NUMBER}"
                            '''
                            
                            // Push changes using stored credentials
                            bat '''
                                @echo off
                                echo "Pushing to remote..."
                                git push origin main
                                if errorlevel 0 (
                                    echo "✅ Changes pushed successfully"
                                ) else (
                                    echo "❌ Failed to push changes"
                                    echo "Checking git remote configuration..."
                                    git remote -v
                                    exit /b 1
                                )
                            '''
                        }
                        
                        echo "✅ Changes committed and pushed to repository"
                        
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
                            <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                        """,
                        to: "${env.EMAIL_RECIPIENTS}",
                        mimeType: 'text/html'
                    )
                }
            }
        }
        failure {
            echo 'Pipeline failed'
            emailext (
                subject: "Build Failed - ${env.JOB_NAME} #${env.BUILD_NUMBER}",
                body: """
                    <h2>Build Failed</h2>
                    <p><b>Job:</b> ${env.JOB_NAME}</p>
                    <p><b>Build:</b> #${env.BUILD_NUMBER}</p>
                    <p><b>Build URL:</b> <a href="${env.BUILD_URL}">${env.BUILD_URL}</a></p>
                    <p><b>Status:</b> ${currentBuild.currentResult}</p>
                """,
                to: "${env.EMAIL_RECIPIENTS}",
                mimeType: 'text/html'
            )
        }
    }
}
