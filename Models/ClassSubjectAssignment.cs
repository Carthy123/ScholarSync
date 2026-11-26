namespace ScholarSync.Models;

public class ClassSubjectAssignment
{
    public int Id { get; set; }
    public int SchoolClassId { get; set; }
    public int SchoolSubjectId { get; set; }
    public SchoolClass? SchoolClass { get; set; }
    public SchoolSubject? SchoolSubject { get; set; }
}
