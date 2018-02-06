# .NET External Quality Check Client

This library provides a .NET API for creating custom quality checks which 
can perform any kind of analysis of your data and record their results 
against a Naveego Data Quality Check.

To get the parameters to pass to the API, create an External Quality Check
in the Naveego Data Quality UI. On the Configuration tab you will be given the
URL, token, and quality check ID you can pass to the API.

For an example of how to use the API, see [Program.cs](https://github.com/naveego/external-quality-check-net/blob/master/Example/Program.cs) 
in the [Example](https://github.com/naveego/external-quality-check-net/tree/master/Example) folder.

To reference this library in your project, run `nuget install Naveego.DQ.ExternalQualityCheck`.