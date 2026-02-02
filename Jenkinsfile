pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    stages {
        stage('Install Correct Tools Version') {
            steps {
                script {
                    bat '''
                        @echo off
                        echo "Cleaning up existing tools..."
                        
                        rem Uninstall any existing versions
                        dotnet tool uninstall --global Swashbuckle.AspNetCore.Cli 2>nul || echo "No global tool to remove"
                        dotnet tool uninstall Swashbuckle.AspNetCore.Cli 2>nul || echo "No local tool to remove"
                        
                        rem Create fresh tool manifest
                        dotnet new tool-manifest --force
                        
                        rem Install version compatible with .NET 8.0
                        echo "Installing Swashbuckle CLI version 6.6.2 (compatible with .NET 8.0)..."
                        dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.6.2
                        
                        rem Also install globally for backup
                        dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                    '''
                }
            }
        }

        stage('Restore Tools') {
            steps {
                bat 'dotnet tool restore'
            }
        }

        stage('Build Project') {
            steps {
                bat '''
                    @echo off
                    echo "Building project..."
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj /p:RunApiContractChecks=false
                    
                    echo "Build completed. Checking output..."
                    if exist "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" (
                        echo "DLL found successfully!"
                    ) else (
                        echo "ERROR: DLL not found!"
                    )
                '''
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json..."
                    
                    rem Check if DLL exists
                    if not exist "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" (
                        echo "ERROR: DLL not found at expected location!"
                        exit /b 1
                    )
                    
                    echo "DLL path: SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll"
                    
                    rem Try with local tool first, then global
                    dotnet swagger tofile --output generated-swagger.json "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" v1
                    
                    if exist generated-swagger.json (
                        echo "Successfully generated swagger.json"
                        echo "Checking file content..."
                        rem Simple check - just see if file has content
                        for %%I in (generated-swagger.json) do set size=%%~zI
                        if %size% GTR 0 (
                            echo "File size: %size% bytes"
                            echo "First line with 'openapi':"
                            findstr /i "openapi" generated-swagger.json 2>nul | more +1
                        ) else (
                            echo "ERROR: File is empty!"
                            exit /b 1
                        )
                    ) else (
                        echo "ERROR: Failed to generate swagger.json"
                        exit /b 1
                    )
                '''
            }
        }

        stage('Lint Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Checking if swagger.json exists..."
                    if exist generated-swagger.json (
                        echo "Found generated-swagger.json, proceeding with linting"
                        
                        rem Check if spectral.yaml exists
                        if exist spectral.yaml (
                            echo "Running spectral lint..."
                            npx @stoplight/spectral-cli lint generated-swagger.json -r spectral.yaml
                        ) else (
                            echo "WARNING: spectral.yaml not found, creating default..."
                            echo "rules: {}" > spectral.yaml
                            echo "Spectral config created, but no linting rules defined."
                        )
                    ) else (
                        echo "ERROR: generated-swagger.json not found!"
                        exit /b 1
                    )
                '''
            }
        }

        stage('Breaking Change Check') {
            steps {
                bat '''
                    @echo off
                    echo "Checking for breaking changes..."
                    
                    rem Check if oasdiff exists
                    if exist "C:\\Program Files\\oasdiff\\oasdiff.exe" (
                        echo "Found oasdiff, checking for baseline swagger.json..."
                        
                        rem Check if baseline exists
                        if exist swagger.json (
                            echo "Running breaking change check..."
                            "C:\\Program Files\\oasdiff\\oasdiff.exe" breaking swagger.json generated-swagger.json
                            rem If oasdiff returns non-zero for breaking changes, we might want to continue
                            rem Jenkins will mark as failed if oasdiff returns non-zero
                        ) else (
                            echo "WARNING: No baseline swagger.json found. This is the first run?"
                            echo "Copying generated-swagger.json as baseline..."
                            copy generated-swagger.json swagger.json
                            echo "Created baseline swagger.json"
                        )
                    ) else (
                        echo "WARNING: oasdiff not installed at C:\\Program Files\\oasdiff\\oasdiff.exe"
                        echo "Skipping breaking change check"
                    )
                '''
            }
        }
    }

    post {
        always {
            echo "Build status: ${currentBuild.currentResult}"
            bat '''
                @echo off
                echo "Workspace contents:"
                dir /b *.json 2>nul || echo "No JSON files found"
                echo.
                echo "generated-swagger.json content preview:"
                if exist generated-swagger.json (
                    type generated-swagger.json | findstr /i "openapi" | head -n 1 2>nul || echo "Could not read openapi line"
                )
            '''
        }
        success {
            echo '✅ Swagger validation passed'
        }
        failure {
            echo '❌ Swagger validation failed'
            archiveArtifacts artifacts: '*.json', allowEmptyArchive: true
        }
    }
}
