using System.Net.Http.Json;
using TmsMvc.Models;

namespace TmsMvc.Services;

public class StudentService
{
    private readonly HttpClient _httpClient;

    public StudentService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("http://localhost:5053/");
    }

    public async Task<List<Student>> GetStudentsAsync()
    {
        return await _httpClient.GetFromJsonAsync<List<Student>>("api/students")
               ?? new List<Student>();
    }

    public async Task<Student?> GetStudentByIdAsync(int id)
    {
        return await _httpClient.GetFromJsonAsync<Student>($"api/students/{id}");
    }

    public async Task CreateStudentAsync(Student student)
    {
        var response = await _httpClient.PostAsJsonAsync("api/students", student);
        response.EnsureSuccessStatusCode();
    }

    public async Task UpdateStudentAsync(int id, Student student)
    {
        var response = await _httpClient.PutAsJsonAsync($"api/students/{id}", student);
        response.EnsureSuccessStatusCode();
    }
    public async Task DeleteStudentAsync(int id)
{
    var response = await _httpClient.DeleteAsync($"api/students/{id}");
    response.EnsureSuccessStatusCode();
}
    
}