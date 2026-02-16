namespace DiagnosticExplorer;

public class Registration
{
    
    public string InstanceId { get; set; }
    public string ProcessName { get; set; }
    public string UserName { get; set; }
    public int Pid { get; set; }
    public string MachineName { get; set; }

    public override string ToString()
    {
        return $"DiagnosticRegistration(UserName:{UserName}, PID:{Pid}, Process:{ProcessName})";
    }
}