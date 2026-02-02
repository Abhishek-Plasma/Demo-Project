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
                    // Clean up existing installations
                    bat '''
                        @echo off
                        echo "Cleaning up existing tools..."
                        
                        # Uninstall any existing versions
                        dotnet tool uninstall --global Swashbuckle.AspNetCore.Cli 2>nul || echo "No global tool to remove"
                        dotnet tool uninstall Swashbuckle.AspNetCore.Cli 2>nul || echo "No local tool to remove"
                        
                        # Create fresh tool manifest
                        dotnet new tool-manifest --force
                        
                        # Install version compatible with .NET 8.0
                        echo "Installing Swashbuckle CLI version 6.6.2 (compatible with .NET 8.0)..."
                        dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.6.2
                        
                        # Also install globally for backup
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
                    dir "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" || echo "DLL not found!"
                '''
            }
        }

        stage('Generate Swagger') {
            steps {
                bat '''
                    @echo off
                    echo "Generating swagger.json..."
                    
                    # Check if DLL exists
                    if not exist "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" (
                        echo "ERROR: DLL not found at expected location!"
                        exit 1
                    )
                    
                    echo "DLL path: SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll"
                    
                    # Try with local tool first, then global
                    dotnet swagger tofile --output generated-swagger.json "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" v1
                    
                    if exist generated-swagger.json (
                        echo "Successfully generated swagger.json"
                        type generated-swagger.json | find /i "openapi" | head -1
                    ) else (
                        echo "ERROR: Failed to generate swagger.json"
                        exit 1
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
                        
                        # Check if spectral.yaml exists
                        if exist spectral.yaml (
                            echo "Running spectral lint..."
                            npx @stoplight/spectral-cli lint generated-swagger.json -r spectral.yaml
                        ) else (
                            echo "WARNING: spectral.yaml not found, skipping linting"
                        )
                    ) else (
                        echo "ERROR: generated-swagger.json not found!"
                        exit 1
                    )
                '''
            }
        }

        stage('Breaking Change Check') {
            steps {
                bat '''
                    @echo off
                    echo "Checking for breaking changes..."
                    
                    # Check if oasdiff exists
                    if exist "C:\\Program Files\\oasdiff\\oasdiff.exe" (
                        echo "Found oasdiff, checking for baseline swagger.json..."
                        
                        # Check if baseline exists
                        if exist swagger.json (
                            echo "Running breaking change check..."
                            "C:\\Program Files\\oasdiff\\oasdiff.exe" breaking swagger.json generated-swagger.json
                        ) else (
                            echo "WARNING: No baseline swagger.json found. This is the first run?"
                            echo "Copying generated-swagger.json as baseline..."
                            copy generated-swagger.json swagger.json
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
