using Microsoft.Extensions.DependencyInjection;

public class EnrollmentWorker
{
    // 'IEnrollmentService'ን በቀጥታ ከመጠቀም ይልቅ 'IServiceScopeFactory'ን ተጠቀም
    private readonly IServiceScopeFactory _scopeFactory;

    public EnrollmentWorker(IServiceScopeFactory scopeFactory)
    {
        _scopeFactory = scopeFactory;
    }

    public void ProcessBatch()
    {
        // አገልግሎቱን ለመጠቀም በፈለግን ጊዜ አዲስ Scope እንፈጥራለን
        using (var scope = _scopeFactory.CreateScope())
        {
            var service = scope.ServiceProvider.GetRequiredService<IEnrollmentService>();
            // አሁን service-ን መጠቀም ትችላለህ
        }
    }
}