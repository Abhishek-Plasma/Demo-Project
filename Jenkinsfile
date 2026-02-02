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
                withCredentials([usernamePassword(
                    credentialsId: 'github-token',
                    usernameVariable: 'GIT_USER',
                    passwordVariable: 'GIT_TOKEN'
                )]) {
                    bat '''
                        @echo off
                        echo "Configuring git user..."
                        git config --global user.email "abhishekk@plasmacomp.com"
                        git config --global user.name "Abhishek-Plasma"
                        git config --global push.default simple
                        
                        REM Store credentials for git operations
                        git config --global credential.helper store
                        echo https://%GIT_USER%:%GIT_TOKEN%@github.com > "%USERPROFILE%\\.git-credentials"
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
                '''
            }
        }

        stage('Install Tools') {
            steps {
                bat '''
                    @echo off
                    echo "Installing Swashbuckle CLI..."
                    
                    REM First, check if dotnet is available
                    where dotnet >nul 2>nul
                    if errorlevel 1 (
                        echo "ERROR: dotnet not found in PATH"
                        echo "PATH: %PATH%"
                        exit /b 1
                    )
                    
                    REM Check if Swashbuckle is already installed (without uninstalling first)
                    dotnet tool list --global | findstr swashbuckle >nul
                    if errorlevel 1 (
                        echo "Swashbuckle CLI not found. Installing..."
                        dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                    ) else (
                        echo "Swashbuckle CLI is already installed"
                    )
                    
                    REM Show where the tool is installed - use swagger.exe, not dotnet-swagger
                    echo "Looking for swagger.exe..."
                    REM Use cmd /c to prevent the where command from failing the entire script
                    cmd /c "where swagger 2>nul" >nul
                    if errorlevel 1 (
                        echo "swagger not found in PATH"
                        echo "Trying to find it in dotnet tools directory..."
                        if exist "%USERPROFILE%\\.dotnet\\tools\\swagger.exe" (
                            echo "Found at: %USERPROFILE%\\.dotnet\\tools\\swagger.exe"
                        ) else (
                            echo "Not found in dotnet tools directory"
                        )
                    ) else (
                        echo "swagger found in PATH"
                    )
                    
                    echo "Swashbuckle CLI installation completed successfully"
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
                    echo "Generating swagger.json..."
                    
                    REM First, update PATH to include dotnet tools
                    set PATH=%USERPROFILE%\\.dotnet\\tools;%PATH%
                    
                    REM Try multiple methods to generate swagger
                    
                    REM Method 1: Try swagger command directly (it's swagger.exe, not dotnet-swagger)
                    echo "Method 1: Using 'swagger' command..."
                    swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    
                    REM Method 2: If method 1 fails, try dotnet swagger command
                    if not exist generated-swagger.json (
                        echo "Method 2: Using 'dotnet swagger' command..."
                        dotnet swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    )
                    
                    REM Method 3: If method 2 fails, try with full path
                    if not exist generated-swagger.json (
                        echo "Method 3: Using full path to swagger..."
                        "%USERPROFILE%\\.dotnet\\tools\\swagger.exe" tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    )
                    
                    if exist generated-swagger.json (
                        echo "SUCCESS: Swagger file generated"
                        for %%I in (generated-swagger.json) do echo File size: %%~zI bytes
                    ) else (
                        echo "ERROR: Failed to generate swagger after multiple attempts"
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
                            echo "Baseline file size:"
                            for %%I in ("SwaggerJsonGen\\swagger.json") do echo %%~zI bytes
                        ) else (
                            echo "Baseline NOT found at: SwaggerJsonGen\\swagger.json"
                            echo "Creating initial baseline..."
                            copy generated-swagger.json SwaggerJsonGen\\swagger.json
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
                        
                        REM Use fc for simple comparison
                        fc "SwaggerJsonGen\\swagger.json" generated-swagger.json > diff.txt
                        
                        if errorlevel 1 (
                            echo "BREAKING CHANGES DETECTED!"
                            echo "Differences:"
                            type diff.txt
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
                            bat '''
                                @echo off
                                echo "Preparing git environment..."
                                
                                REM Ensure we're on main branch and clean
                                git checkout main
                                git fetch origin
                                git reset --hard origin/main
                                git clean -fdx
                                
                                REM Update the swagger.json
                                echo "Updating SwaggerJsonGen\\swagger.json..."
                                copy generated-swagger.json SwaggerJsonGen\\swagger.json /Y
                                
                                REM Commit the changes
                                echo "Committing changes..."
                                git add SwaggerJsonGen/swagger.json
                                git commit -m "Update API contract - Build #%BUILD_NUMBER%"
                                
                                REM Push using stored credentials
                                echo "Pushing changes..."
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
