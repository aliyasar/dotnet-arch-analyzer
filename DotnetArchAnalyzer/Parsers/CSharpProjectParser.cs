namespace DotnetArchAnalyzer.Parsers
{
    public class CSharpProjectParser
    {
        public List<string> GetCSharpFiles(string projectPath)
        {
            var files = Directory.GetFiles(projectPath, "*.cs", SearchOption.AllDirectories)
                .Where(f => !f.Contains("bin") && !f.Contains("obj"));
            return files.ToList();
        }
    }
}
