pipeline {
    agent any

    options {
        disableConcurrentBuilds()
        buildDiscarder(logRotator(numToKeepStr: '10'))
    }

    stages {
        stage('Clean & Setup') {
            steps {
                bat '''
                    @echo off
                    echo "Cleaning previous generated files..."
                    if exist generated-swagger.json del generated-swagger.json
                    
                    echo "Checking for baseline swagger.json..."
                    if exist swagger.json (
                        echo "Baseline exists: swagger.json"
                        for %%I in (swagger.json) do echo Baseline size: %%~zI bytes
                    ) else (
                        echo "No baseline found (first run)"
                    )
                '''
            }
        }

        stage('Install Tools') {
            steps {
                bat '''
                    @echo off
                    echo "Installing Swashbuckle CLI..."
                    dotnet tool install --global Swashbuckle.AspNetCore.Cli --version 6.6.2
                    
                    echo "Setting up local tools..."
                    dotnet new tool-manifest --force
                    dotnet tool install Swashbuckle.AspNetCore.Cli --version 6.6.2
                '''
            }
        }

        stage('Build Project') {
            steps {
                bat '''
                    @echo off
                    echo "Building project..."
                    dotnet build SwaggerJsonGen\\SwaggerJsonGen.csproj /p:RunApiContractChecks=false
                    
                    if exist "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" (
                        echo "SUCCESS: Build completed"
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
                    echo "Generating Swagger JSON..."
                    dotnet swagger tofile --output generated-swagger.json "SwaggerJsonGen\\bin\\Debug\\net8.0\\SwaggerJsonGen.dll" v1
                    
                    if exist generated-swagger.json (
                        echo "SUCCESS: Swagger JSON generated"
                        for %%I in (generated-swagger.json) do echo File size: %%~zI bytes
                        
                        echo "Sample of generated swagger:"
                        type generated-swagger.json | findstr /i "openapi title description version" | head -5
                    ) else (
                        echo "ERROR: Failed to generate Swagger JSON"
                        exit /b 1
                    )
                '''
            }
        }

        stage('Breaking Change Analysis') {
            steps {
                script {
                    echo "=== BREAKING CHANGE ANALYSIS ==="
                    
                    // Check if we have both files for comparison
                    bat '''
                        @echo off
                        echo "Checking files for comparison..."
                        
                        if not exist generated-swagger.json (
                            echo "ERROR: No new swagger file generated"
                            exit /b 1
                        )
                        
                        if not exist swagger.json (
                            echo "WARNING: No baseline swagger.json found"
                            echo "This is the first run - creating baseline..."
                            copy generated-swagger.json swagger.json
                            echo "Baseline created from current version"
                            exit /b 0
                        )
                        
                        echo "Both files exist for comparison:"
                        echo "  Baseline: swagger.json"
                        echo "  New: generated-swagger.json"
                    '''
                    
                    // Try different methods to detect changes
                    bat '''
                        @echo off
                        echo.
                        echo "=== METHOD 1: Simple file comparison ==="
                        fc swagger.json generated-swagger.json > diff.txt 2>nul
                        if errorlevel 1 (
                            echo "DIFFERENCES FOUND (simple comparison):"
                            type diff.txt
                        ) else (
                            echo "No differences found (files are identical)"
                        )
                        
                        echo.
                        echo "=== METHOD 2: File size comparison ==="
                        for %%I in (swagger.json) do set baseline_size=%%~zI
                        for %%I in (generated-swagger.json) do set new_size=%%~zI
                        echo "Baseline size: !baseline_size! bytes"
                        echo "New size: !new_size! bytes"
                        if !baseline_size! neq !new_size! (
                            echo "WARNING: File sizes differ - potential changes"
                        ) else (
                            echo "File sizes match"
                        )
                    '''
                    
                    // Try oasdiff if available
                    bat '''
                        @echo off
                        echo.
                        echo "=== METHOD 3: OASDiff Tool ==="
                        
                        rem Check if oasdiff is installed
                        where oasdiff 2>nul
                        if errorlevel 0 (
                            echo "Found oasdiff in PATH"
                            set OASDIFF=oasdiff
                            goto runOasdiff
                        )
                        
                        if exist "C:\\Program Files\\oasdiff\\oasdiff.exe" (
                            echo "Found oasdiff at C:\\Program Files\\oasdiff\\oasdiff.exe"
                            set OASDIFF="C:\\Program Files\\oasdiff\\oasdiff.exe"
                            goto runOasdiff
                        )
                        
                        if exist "C:\\Tools\\oasdiff\\oasdiff.exe" (
                            echo "Found oasdiff at C:\\Tools\\oasdiff\\oasdiff.exe"
                            set OASDIFF="C:\\Tools\\oasdiff\\oasdiff.exe"
                            goto runOasdiff
                        )
                        
                        echo "oasdiff not found. Installing temporarily..."
                        
                        rem Try to download oasdiff
                        curl -L -o oasdiff_windows_amd64.zip https://github.com/Tufin/oasdiff/releases/latest/download/oasdiff_windows_amd64.zip 2>nul
                        if exist oasdiff_windows_amd64.zip (
                            tar -xf oasdiff_windows_amd64.zip oasdiff.exe
                            set OASDIFF=oasdiff.exe
                            echo "Installed oasdiff from GitHub release"
                        ) else (
                            echo "Could not download oasdiff. Skipping semantic diff."
                            goto end
                        )
                        
                        :runOasdiff
                        echo "Running oasdiff breaking change detection..."
                        echo "Command: %OASDIFF% breaking swagger.json generated-swagger.json"
                        
                        %OASDIFF% breaking swagger.json generated-swagger.json > oasdiff_output.txt 2>&1
                        
                        if errorlevel 0 (
                            echo "SUCCESS: No breaking changes detected by oasdiff"
                            type oasdiff_output.txt
                        ) else (
                            echo "BREAKING CHANGES DETECTED by oasdiff:"
                            type oasdiff_output.txt
                            
                            echo.
                            echo "=== BREAKING CHANGE SUMMARY ==="
                            echo "The API has breaking changes that could break existing clients."
                            echo "Review the changes above before deploying."
                        )
                        
                        :end
                        echo "Breaking change analysis complete"
                    '''
                }
            }
        }
    }

    post {
        always {
            echo "=== BUILD SUMMARY ==="
            echo "Status: ${currentBuild.currentResult}"
            
            bat '''
                @echo off
                echo.
                echo "Generated files:"
                dir *.json 2>nul || echo "No JSON files"
                echo.
                echo "Diff files:"
                dir diff.txt oasdiff_output.txt 2>nul || echo "No diff files"
                
                echo.
                echo "=== RECOMMENDATION ==="
                if exist diff.txt (
                    echo "Changes detected in API definition."
                    echo "If this is intentional, update the baseline:"
                    echo "  copy generated-swagger.json swagger.json"
                ) else (
                    echo "No changes detected. API is stable."
                )
            '''
            
            // Archive artifacts for review
            archiveArtifacts artifacts: '*.json, *.txt', allowEmptyArchive: true
        }
        success {
            echo '✅ Pipeline completed successfully'
        }
        failure {
            echo '❌ Pipeline failed'
        }
    }
}
