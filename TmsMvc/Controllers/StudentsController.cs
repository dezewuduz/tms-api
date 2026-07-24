using Microsoft.AspNetCore.Mvc;
using TmsMvc.Services;
using TmsMvc.Models;

namespace TmsMvc.Controllers;

public class StudentsController : Controller
{
    private readonly StudentService _studentService;

    public StudentsController(StudentService studentService)
    {
        _studentService = studentService;
    }

    public async Task<IActionResult> Index()
    {
        var students = await _studentService.GetStudentsAsync();
        return View(students);
    }

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Student student)
    {
        if (!ModelState.IsValid)
        {
            return View(student);
        }

        await _studentService.CreateStudentAsync(student);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);

        if (student is null)
        {
            return NotFound();
        }

        return View(student);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Student student)
    {
        if (id != student.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            return View(student);
        }

        await _studentService.UpdateStudentAsync(id, student);

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var student = await _studentService.GetStudentByIdAsync(id);

        if (student is null)
        {
            return NotFound();
        }

        return View(student);
    }

    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _studentService.DeleteStudentAsync(id);

        return RedirectToAction(nameof(Index));
    }
    [HttpGet]
public async Task<IActionResult> Details(int id)
{
    var student = await _studentService.GetStudentByIdAsync(id);

    if (student == null)
    {
        return NotFound();
    }

    return View(student);
}
}