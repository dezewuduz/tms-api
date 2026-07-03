namespace TmsApi.Services;

public class DuplicateEnrollmentException(int studentId, int courseId)
    : Exception($"Student {studentId} is already enrolled in course {courseId}.");