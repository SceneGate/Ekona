using Cake.Core;
using Cake.Frosting.PleOps.Recipe;

namespace BuildSystem;

public class BuildContext : PleOpsBuildContext
{
    public BuildContext(ICakeContext context) : base(context)
    {
        TestResourceUri = string.Empty;
    }

    public string TestResourceUri { get; set; }

    public override void ReadArguments()
    {
        base.ReadArguments();

        Arguments.SetIfPresent("resource-uri", x => TestResourceUri = x);
    }
}
