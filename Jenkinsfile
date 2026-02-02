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
                    if exist diff.txt del diff.txt
                    if exist oasdiff_output.txt del oasdiff_output.txt
                    
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
                        setlocal enabledelayedexpansion
                        set count=0
                        for /f "tokens=*" %%a in (generated-swagger.json) do (
                            echo | findstr /i "openapi title description version" <nul >nul && (
                                echo %%a
                                set /a count+=1
                            )
                            if !count! equ 5 exit /b 0
                        )
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
                    
                    // Simple file comparison
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
                        setlocal enabledelayedexpansion
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
                        
                        rem Check if oasdiff is installed at default location
                        if exist "C:\\Program Files\\oasdiff\\oasdiff.exe" (
                            echo "Found oasdiff at C:\\Program Files\\oasdiff\\oasdiff.exe"
                            set OASDIFF="C:\\Program Files\\oasdiff\\oasdiff.exe"
                            goto runOasdiff
                        )
                        
                        echo "oasdiff not found at default location."
                        echo "Checking if oasdiff is in PATH..."
                        where oasdiff 2>nul
                        if errorlevel 0 (
                            echo "Found oasdiff in PATH"
                            set OASDIFF=oasdiff
                            goto runOasdiff
                        )
                        
                        echo "oasdiff not available. Skipping semantic diff analysis."
                        echo "Note: Install oasdiff for better breaking change detection."
                        goto end
                        
                        :runOasdiff
                        echo "Running oasdiff breaking change detection..."
                        echo "Command: %OASDIFF% breaking swagger.json generated-swagger.json"
                        
                        %OASDIFF% breaking swagger.json generated-swagger.json > oasdiff_output.txt 2>&1
                        set oasdiff_exit_code=%errorlevel%
                        
                        if %oasdiff_exit_code% == 0 (
                            echo "SUCCESS: No breaking changes detected by oasdiff"
                            type oasdiff_output.txt
                        ) else (
                            echo "BREAKING CHANGES DETECTED by oasdiff (exit code: %oasdiff_exit_code%):"
                            type oasdiff_output.txt
                            
                            echo.
                            echo "=== BREAKING CHANGE SUMMARY ==="
                            echo "The API has breaking changes that could break existing clients."
                            echo "Review the changes above before deploying."
                            echo.
                            echo "If these changes are intentional, update the baseline with:"
                            echo "  copy generated-swagger.json swagger.json"
                        )
                        
                        :end
                        echo "Breaking change analysis complete"
                    '''
                }
            }
        }
        
        stage('Update Baseline') {
            steps {
                script {
                    // Ask if we should update the baseline
                    echo "Checking if baseline should be updated..."
                    
                    bat '''
                        @echo off
                        echo "Current baseline: swagger.json"
                        echo "New API definition: generated-swagger.json"
                        
                        rem Check if files are different
                        fc swagger.json generated-swagger.json > nul
                        if errorlevel 1 (
                            echo "Files are different. Options:"
                            echo "1. Keep current baseline (no action)"
                            echo "2. Update baseline with new version (copy generated-swagger.json to swagger.json)"
                            echo.
                            echo "For now, we'll keep the current baseline."
                            echo "To update manually, run: copy generated-swagger.json swagger.json"
                        ) else (
                            echo "Files are identical - no update needed"
                        )
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
                echo "Generated files in workspace:"
                dir *.json 2>nul || echo "No JSON files"
                
                if exist diff.txt (
                    echo.
                    echo "=== DIFFERENCES DETECTED ==="
                    echo "The new API differs from the baseline."
                    echo "Review diff.txt for details."
                )
                
                if exist oasdiff_output.txt (
                    echo.
                    echo "=== OASDIFF OUTPUT ==="
                    type oasdiff_output.txt
                )
                
                echo.
                echo "=== NEXT STEPS ==="
                if exist swagger.json (
                    echo "Baseline: swagger.json"
                )
                if exist generated-swagger.json (
                    echo "New API: generated-swagger.json"
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
