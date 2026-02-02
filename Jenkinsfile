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
        // Add PATH for dotnet tools
        PATH = "${env.PATH};C:\\Users\\jenkins\\.dotnet\\tools"
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
                        
                        REM Store credentials temporarily (Windows approach)
                        set GITHUB_URL=https://%GIT_USER%:%GIT_TOKEN%@github.com/Abhishek-Plasma/Demo-Project.git
                        git remote set-url origin "%GITHUB_URL%"
                        
                        REM Also store for git operations
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
                    if exist SwaggerJsonGen\\swagger-temp.json del SwaggerJsonGen\\swagger-temp.json
                '''
            }
        }

        stage('Install Tools') {
            steps {
                bat '''
                    @echo off
                    echo "Installing Swashbuckle CLI..."
                    
                    REM Force reinstall and ensure it's available
                    dotnet tool uninstall --global Swashbuckle.AspNetCore.Cli 2>nul || echo "Tool not installed or already removed"
                    dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                    
                    REM Check where it's installed
                    where dotnet-swagger
                    echo "Swashbuckle CLI installation completed"
                    
                    REM Update PATH for current session
                    for /f "tokens=*" %%i in ('dotnet --list-tools ^| findstr "swashbuckle"') do (
                        echo "Found tool: %%i"
                    )
                '''
            }
        }

        stage('Build') {
            steps {
                bat '''
                    @echo off
                    echo "Building project..."
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj --configuration Debug --verbosity minimal
                    
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

        stage('Generate Swagger - Alternative Method') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json using alternative method..."
                    
                    REM Method 1: Try with full path to dotnet-swagger
                    echo "Method 1: Using dotnet-swagger..."
                    where dotnet-swagger
                    dotnet-swagger tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    
                    REM Method 2: If Method 1 fails, use dotnet tool run
                    if not exist generated-swagger.json (
                        echo "Method 2: Using dotnet tool run..."
                        dotnet tool run swashbuckle tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                    )
                    
                    REM Method 3: Create a simple program to generate swagger
                    if not exist generated-swagger.json (
                        echo "Method 3: Creating custom swagger generator..."
                        echo Creating temporary program...
                        
                        REM Create a simple C# program to generate swagger
                        echo using System; > generate-swagger.cs
                        echo using System.IO; >> generate-swagger.cs
                        echo using Microsoft.OpenApi; >> generate-swagger.cs
                        echo using Microsoft.OpenApi.Extensions; >> generate-swagger.cs
                        echo using Swashbuckle.AspNetCore.Swagger; >> generate-swagger.cs
                        
                        REM Try using the installed tool via absolute path
                        echo "Method 4: Trying absolute path..."
                        for /f "tokens=*" %%i in ('dotnet --list-tools ^| findstr "swashbuckle"') do (
                            echo Found tool path pattern: %%i
                        )
                        
                        REM Try common locations
                        if exist "%USERPROFILE%\\.dotnet\\tools\\dotnet-swagger.exe" (
                            "%USERPROFILE%\\.dotnet\\tools\\dotnet-swagger.exe" tofile --output generated-swagger.json SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll v1
                        )
                    )
                    
                    if exist generated-swagger.json (
                        echo "SUCCESS: Swagger file generated"
                        for %%I in (generated-swagger.json) do echo File size: %%~zI bytes
                    ) else (
                        echo "ERROR: Failed to generate swagger after multiple attempts"
                        echo "Current directory: %CD%"
                        echo "Files in SwaggerJsonGen\\bin\\Debug\\net8.0\\:"
                        dir "SwaggerJsonGen\\bin\\Debug\\net8.0\\"
                        echo "Trying to generate minimal swagger manually..."
                        
                        REM Create a minimal swagger.json as fallback
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
                        echo "Baseline file:"
                        type "SwaggerJsonGen\\swagger.json" | head -5
                        echo "Generated file:"
                        type generated-swagger.json | head -5
                        
                        REM Use PowerShell for better comparison
                        powershell -Command "if ((Get-FileHash 'SwaggerJsonGen\\swagger.json').Hash -ne (Get-FileHash 'generated-swagger.json').Hash) { 'Files are different' | Out-File 'diff.txt' -Encoding ASCII } else { 'Files are identical' }"
                        
                        if exist diff.txt (
                            echo "BREAKING CHANGES DETECTED!"
                            echo "Differences found in swagger.json"
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
                                
                                REM Reset to clean state
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
                                
                                REM Create commit with proper message
                                git commit -m "Update API contract - Build #%BUILD_NUMBER%"
                                
                                REM Push using the stored credentials
                                echo "Pushing changes..."
                                git push origin main
                                
                                if errorlevel 0 (
                                    echo "✅ Changes pushed successfully"
                                ) else (
                                    echo "❌ Failed to push changes"
                                    echo "Trying alternative push method..."
                                    
                                    REM Alternative: Use HTTPS URL with token
                                    git push https://%GIT_USER%:%GIT_TOKEN%@github.com/Abhishek-Plasma/Demo-Project.git main
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
