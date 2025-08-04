# TS4Tools Cross-Platform Implementation Plan

**Document Version:** 1.0  
**Created:** August 3, 2025  
**Status:** Active Implementation Plan  

## 🎯 **Executive Summary**

TS4Tools has an **excellent cross-platform foundation** with .NET 9 + Avalonia UI, but has **3 critical issues** that need immediate attention to achieve full cross-platform deployment capability.

**Overall Rating:** 🟢 **EXCELLENT** (Foundation) + 🟡 **NEEDS FIXES** (Platform-specific issues)

## 🔴 **Critical Issues Requiring Immediate Action**

### **Issue #1: Windows-Only Application Manifest (HIGH PRIORITY)**

**File:** `TS4Tools.Desktop\app.manifest`  
**Problem:** Contains Windows 10+ only declarations that prevent cross-platform deployment  
**Fix Timeline:** Phase 3.2 (immediate)

**Current Code:**
```xml
<supportedOS Id="{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}" />
```

**Recommended Fix:**
```xml
<Project>
  <!-- Use manifest only on Windows -->
  <PropertyGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
    <ApplicationManifest>app.manifest</ApplicationManifest>
  </PropertyGroup>
</Project>
```

### **Issue #2: Windows-Only Reserved Filename Checking (MEDIUM PRIORITY)**

**File:** `src\TS4Tools.Extensions\Utilities\FileNameService.cs`  
**Problem:** Only validates Windows reserved names (CON, PRN, AUX), may cause issues on Unix systems  
**Fix Timeline:** Phase 4.1 (near-term)

**Current Code:**
```csharp
private static bool IsReservedName(string name)
{
    var upperName = name.ToUpperInvariant();
    return upperName is "CON" or "PRN" or "AUX" or "NUL" or
           "COM1" or "COM2" or ... // Windows-only
}
```

**Recommended Fix:**
```csharp
private static bool IsReservedName(string name)
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
    {
        var upperName = name.ToUpperInvariant();
        return upperName is "CON" or "PRN" or "AUX" or "NUL" or
               "COM1" or "COM2" or ... // Windows reserved names
    }
    
    // Unix systems have different restrictions (e.g., case sensitivity)
    return name.Contains('\0') || name == "." || name == "..";
}
```

### **Issue #3: Windows-Centric Configuration Directories (MEDIUM PRIORITY)**

**File:** `src\TS4Tools.Core.System\Configuration\PortableConfiguration.cs`  
**Problem:** Uses only `Environment.SpecialFolder.ApplicationData` (Windows-centric)  
**Fix Timeline:** Phase 4.1 (near-term)

**Current Code:**
```csharp
private static string GetDefaultConfigurationDirectory()
{
    return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
}
```

**Recommended Fix:**
```csharp
private static string GetDefaultConfigurationDirectory()
{
    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    
    if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), 
                           "Library", "Preferences");
    
    // Linux/Unix
    return Environment.GetEnvironmentVariable("XDG_CONFIG_HOME") ?? 
           Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".config");
}
```

## ✅ **Excellent Cross-Platform Foundation Already in Place**

**Architecture Strengths:**
- ✅ **Modern .NET 9** - Full cross-platform support
- ✅ **Avalonia UI** - Native cross-platform UI framework
- ✅ **JSON Configuration** - Replaces Windows Registry dependencies
- ✅ **Path.Combine() Usage** - Platform-neutral file path handling
- ✅ **Central Package Management** - Consistent dependencies across platforms
- ✅ **Modern File APIs** - Uses cross-platform System.IO methods

## 🛠️ **Implementation Strategy**

### **Phase 3.2 - Immediate Fixes (Week 1)**
1. **Fix Application Manifest** - Make Windows-conditional
2. **Add Platform Detection Service** - IPlatformService interface
3. **Set up Cross-Platform CI/CD** - GitHub Actions for Windows/macOS/Linux

### **Phase 4.1 - Enhanced Platform Support (Week 2-3)**
1. **Platform-Specific Configuration Directories** - Proper config paths for each OS
2. **Enhanced File Name Validation** - Platform-aware filename sanitization
3. **Platform-Specific UI Adaptations** - Menu conventions, keyboard shortcuts

### **Phase 5+ - Advanced Cross-Platform Features (Future)**
1. **Automated Cross-Platform Testing** - Continuous testing on all platforms
2. **Platform-Specific Optimizations** - Native API integrations where beneficial
3. **Native Platform Integrations** - File associations, system tray, etc.

## 📋 **Verification Checklist**

**Immediate Verification (Phase 3.2):**
- [ ] Application builds successfully on Windows, macOS, Linux
- [ ] No platform-specific compilation errors
- [ ] Basic UI functionality works on all platforms
- [ ] Configuration files created in appropriate locations

**Enhanced Verification (Phase 4.1):**
- [ ] File operations handle platform differences correctly
- [ ] Configuration persists correctly on all platforms
- [ ] UI follows platform conventions
- [ ] File name validation works correctly on all platforms

**Quality Assurance Commands:**
```powershell
# Windows Testing
cd "c:\Users\nawgl\code\TS4Tools"
dotnet build
dotnet test
dotnet run --project TS4Tools.Desktop

# Cross-Platform CI/CD Pipeline
# Will be set up in GitHub Actions for automated testing
```

## 🎯 **Success Metrics**

**Phase 3.2 Success Criteria:**
- ✅ Clean builds on Windows, macOS, Linux
- ✅ No platform-specific warnings or errors
- ✅ Basic functionality verified on all platforms

**Phase 4.1 Success Criteria:**
- ✅ Platform-appropriate configuration storage
- ✅ Correct file name validation on all platforms
- ✅ Platform-consistent UI behavior

**Long-term Success Criteria:**
- ✅ Automated testing pipeline for all platforms
- ✅ Performance benchmarks on all platforms
- ✅ Production deployment capabilities for all platforms

---

**Implementation Status:** 🔄 **IN PROGRESS**  
**Next Action:** Fix application manifest in Phase 3.2  
**Owner:** Development Team  
**Review Date:** After Phase 3.2 completion
