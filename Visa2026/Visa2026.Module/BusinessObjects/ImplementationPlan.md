# Implementation Plan: Incorporating the core PDF form filling functionality  Project

This document outlines the plan for incorporating the VISA2014 project (referred to as "the injected project") into our existing project.

## 1. Goals

*   Integrate the core PDF form filling functionality from the VISA2014 project.
*   Avoid direct dependencies on the injected project's specific data objects.
*   Create a robust, maintainable, and testable solution.
*   Adhere to our project's existing coding standards and architectural principles.

## 2. Technology Stack

*   **Programming Language:** C#
*   **PDF Library:** Spire.Pdf (already used in the injected project)
*   **Dependency Injection:** [Your DI Container - e.g., Autofac, Ninject, Microsoft.Extensions.DependencyInjection]
*   **Build Tool:** [Your Build Tool - e.g., MSBuild, Cake]
*   **UI Framework**: [Your UI Framework - e.g., ASP.NET Web Forms, ASP.NET MVC, Blazor]
*   **Version Control:** Git

## 3. Implementation Steps


1.  **Code Extraction and Refactoring:** [COMPLETED]
    *   Identify the core PDF filling logic in `HelperFillForm.cs`.
    *   Extract this logic into a new, independent class (e.g., `PdfFormFillerService`).
    *   Remove direct dependencies on `IPersonInApplication` and other injected project-specific types. Instead, use a generic data transfer object (DTO) or a dictionary to pass data to the form filler.
    *   Implement interfaces to abstract away Spire.Pdf implementation for better testability.
2.  **Dependency Injection:** [COMPLETED]
    *   Register the `PdfFormFillerService` in our project's DI container.
    
3.  **Create Custom Action:** [COMPLETED]
    * Implement a custom action (button) in your UI based on your UI Framework on the `Application` DetailView to trigger PDF generation.
4.  **PDF Generation logic:** [COMPLETED]
    * Implement PDF generation logic in the custom action to use the `PdfFormFillerService` and pass data from `Application` and `ApplicationItem` objects.
    *   Inject the `PdfFormFillerService` into the components that need to fill PDF forms.
3.  **Data Mapping:** [COMPLETED]
    *   Create a mapping layer to transform our project's data objects into the generic DTO or dictionary format required by the `PdfFormFillerService`.
4.  **Configuration:** [COMPLETED]
    *   Externalize the PDF form template path to `appsettings.json` under the key `PdfSettings:TemplatePath`.
    *   Place the template PDF (`Visa_Application_TM_QR_08.pdf`) in the `Visa2026.Module/Resources` folder.
    *   Set the PDF file's build action to **Content** and **Copy to Output Directory** to **Copy if newer**.
5.  **Error Handling and Logging:** [COMPLETED]
    *   Implement robust error handling with `try-catch` blocks to gracefully handle potential exceptions.  Examples:
        *   `FileNotFoundException`: When the PDF template file is not found at the configured path.
        *   `IOException`: When there are issues reading or writing PDF files.
        *   `Spire.Pdf.PdfException`:  For any errors specific to Spire.Pdf library
        *   `NullReferenceException`: If `Application`, `ApplicationItem`, or properties within them are null. Add null checks or handle this exception.
        *   `FormatException`:  If there are issues parsing dates or converting data types.
        *   `ConfigurationErrorsException`: If there are any issues with configuration file loading or format.
    *   Integrate with our project's existing logging framework.
    *   Log all caught exceptions, including relevant details (file paths, field names, data values) to aid in debugging.
6.  **Testing:**
    *   Ensure that tests cover exception scenarios to verify that error handling works correctly.
    *   Test with invalid data to ensure data validation and error handling are implemented
    *   Write unit tests for the `PdfFormFillerService` and the data mapping layer.
7.  **Deployment:**
    *   Package and deploy the updated application.

## 4. Tools

*   Visual Studio 2026
* [Your Unit Testing Framework - e.g., NUnit, xUnit]
* [Your Logging Framework - e.g., Serilog, NLog]