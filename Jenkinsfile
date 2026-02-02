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
        DOTNET_CLI_HOME = 'C:\\ProgramData\\dotnet'  // Explicitly set dotnet home
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
                        
                        REM Set up credential helper
                        git config --global credential.helper store
                        
                        REM Create credential file
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
                    
                    REM Check if dotnet tool is already installed
                    dotnet tool list --global | findstr swashbuckle
                    if errorlevel 1 (
                        echo "Installing Swashbuckle CLI..."
                        dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                    ) else (
                        echo "Swashbuckle CLI is already installed"
                    )
                    
                    REM Update PATH to include dotnet tools
                    set PATH=%USERPROFILE%\\.dotnet\\tools;%PATH%
                    echo "Updated PATH: %PATH%"
                '''
            }
        }

        stage('Build') {
            steps {
                bat '''
                    @echo off
                    echo "Building project..."
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj --configuration Debug
                '''
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json..."
                    
                    REM Update PATH to ensure dotnet tools are available
                    set PATH=%USERPROFILE%\\.dotnet\\tools;%PATH%
                    
                    REM First, check if the tool is available
                    where dotnet-swagger
                    if errorlevel 1 (
                        echo "ERROR: dotnet-swagger not found in PATH"
                        echo "Trying alternate method..."
                    )
                    
                    REM Try multiple ways to run swagger generation
                    echo "Attempt 1: Using dotnet swagger command..."
                    dotnet swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    
                    if not exist generated-swagger.json (
                        echo "Attempt 2: Using dotnet-swagger directly..."
                        dotnet-swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    )
                    
                    if not exist generated-swagger.json (
                        echo "Attempt 3: Using full path to dotnet-swagger..."
                        "%USERPROFILE%\\.dotnet\\tools\\dotnet-swagger.exe" tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    )
                    
                    if exist generated-swagger.json (
                        echo "SUCCESS: Swagger file generated"
                        for %%I in (generated-swagger.json) do echo File size: %%~zI bytes
                    ) else (
                        echo "ERROR: Failed to generate swagger"
                        echo "DLL Path: SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll"
                        echo "Current Directory: %CD%"
                        echo "PATH: %PATH%"
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
                        REM Use a proper diff tool or PowerShell for better comparison
                        powershell -Command "Compare-Object (Get-Content 'SwaggerJsonGen\\swagger.json') (Get-Content 'generated-swagger.json') | Out-File 'diff.txt' -Encoding ASCII"
                        
                        if exist diff.txt (
                            REM Check if diff.txt has content
                            for /f %%i in ('powershell -Command "(Get-Content 'diff.txt' | Measure-Object -Line).Lines"') do set lineCount=%%i
                            if !lineCount! GTR 0 (
                                echo "BREAKING CHANGES DETECTED!"
                                type diff.txt
                                exit /b 0
                            ) else (
                                echo "No breaking changes detected"
                                del diff.txt
                                exit /b 0
                            )
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
                            // First, reset and clean the repository
                            bat '''
                                @echo off
                                echo "Resetting repository..."
                                git checkout main
                                git clean -fdx
                                git reset --hard HEAD
                                git pull origin main
                                git status
                            '''
                            
                            // Update the file
                            bat '''
                                @echo off
                                echo "Updating SwaggerJsonGen\\swagger.json..."
                                copy generated-swagger.json SwaggerJsonGen\\swagger.json /Y
                            '''
                            
                            // Commit changes
                            bat """
                                @echo off
                                echo "Committing changes..."
                                git add SwaggerJsonGen/swagger.json
                                git commit -m "${params.COMMIT_MESSAGE} - Build #${env.BUILD_NUMBER}"
                            """
                            
                            // Push changes using stored credentials
                            bat '''
                                @echo off
                                echo "Pushing to remote..."
                                git push origin main
                                echo "✅ Changes pushed successfully"
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
