#!/bin/bash

# Find a single small package file for testing
TEST_PACKAGE=$(find "/home/dez/snap/steam/common/.local/share/Steam/steamapps/common/The Sims 4" -name "*.package" -size +1k -size -10M | head -1)

echo "Testing with package: $TEST_PACKAGE"
echo "Package size: $(du -h "$TEST_PACKAGE" | cut -f1)"

# Create a temporary config for testing with just one package
cd /home/dez/code/TS4Tools

# Temporarily modify the game directory to test with a single package
mkdir -p temp_test_dir
cp "$TEST_PACKAGE" temp_test_dir/

# Update the config temporarily
cat > temp_appsettings.json << EOF
{
  "GameDirectory": "/home/dez/code/TS4Tools/temp_test_dir",
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "TS4Tools.Core.Package": "Information",
      "TS4Tools.Extensions.ResourceTypes": "Information"
    }
  }
}
EOF

# Run the analysis
timeout 60 dotnet run --project PackageAnalysisScript/PackageAnalysisScript.csproj

# Clean up
rm -rf temp_test_dir temp_appsettings.json
