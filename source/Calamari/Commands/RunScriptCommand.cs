﻿using System.IO;
using Calamari.Commands.Support;
using Calamari.Extensibility;
using Calamari.Features;
using Calamari.Util;

namespace Calamari.Commands
{
    [Command("run-script", Description = "Invokes a PowerShell or ScriptCS script")]
    public class RunScriptCommand : Command
    {
        private string variablesFile;
        private string scriptFile;
        private string sensitiveVariablesFile;
        private string sensitiveVariablesPassword;
        private string packageFile;
        private bool substituteVariables;
        private string scriptParameters;

        public RunScriptCommand()
        {
            Options.Add("variables=", "Path to a JSON file containing variables.", v => variablesFile = Path.GetFullPath(v));
            Options.Add("package=", "Path to the package to extract that contains the package.", v => packageFile = Path.GetFullPath(v));
            Options.Add("script=", "Path to the script to execute. If --package is used, it can be a script inside the package.", v => scriptFile = Path.GetFullPath(v));
            Options.Add("scriptParameters=", "Parameters to pass to the script.", v => scriptParameters = v);
            Options.Add("sensitiveVariables=", "Password protected JSON file containing sensitive-variables.", v => sensitiveVariablesFile = v);
            Options.Add("sensitiveVariablesPassword=", "Password used to decrypt sensitive-variables.", v => sensitiveVariablesPassword = v);
            Options.Add("substituteVariables", "Perform variable substitution on the script body before executing it.", v => substituteVariables = true);
        }

        public override int Execute(string[] commandLineArguments)
        {
            Options.Parse(commandLineArguments);

            var variables = new CalamariVariableDictionary(variablesFile, sensitiveVariablesFile, sensitiveVariablesPassword);
            variables.Set(SpecialVariables.OriginalPackageDirectoryPath, CrossPlatform.GetCurrentDirectory());
            variables.Set(SpecialVariables.Package.SubstituteInFilesEnabled, substituteVariables.ToString());

            variables.Set(SpecialVariables.Package.SubstituteInFilesTargets, scriptFile);
            variables.Set(SpecialVariables.Action.Script.Path, scriptFile);

            variables.Set(SpecialVariables.Action.Script.PackagePath, packageFile);
            variables.Set(SpecialVariables.Action.Script.Parameters, scriptParameters);

            return new FeatureRunCommand().Execute("Calamari.Features.RunScriptFeature, Calamari", variables);            
        }
    }
}