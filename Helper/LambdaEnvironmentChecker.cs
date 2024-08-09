using System;

namespace NuevaLuz.Fonoteca.Helper
{
    public class LambdaEnvironmentChecker
    {
        public static bool IsRunningInLambda()
        {
            string lambdaFunctionName = Environment.GetEnvironmentVariable("AWS_LAMBDA_FUNCTION_NAME");
            return !string.IsNullOrEmpty(lambdaFunctionName);
        }
    }
}
