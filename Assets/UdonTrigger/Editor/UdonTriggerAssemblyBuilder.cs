using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;

public class UdonTriggerAssemblyBuilder
{
    protected StringBuilder _assemblyTextBuilder = new StringBuilder();
    protected int indentLevel = 1;

    protected string GetAssemblyStr()
    {
        return _assemblyTextBuilder.ToString();
    }

    protected void AppendLine(string line)
    {
        _assemblyTextBuilder.Append($"{new string(' ', indentLevel * 8)}{line}");
        _assemblyTextBuilder.Append("\n");
    }

    protected void AddNop() {
        _assemblyTextBuilder.Append("NOP");
        _assemblyTextBuilder.Append("\n");
    }

    protected void AddPush()
    {

    }
}

public class UdonTriggerEventBuilder : UdonTriggerAssemblyBuilder
{

}
