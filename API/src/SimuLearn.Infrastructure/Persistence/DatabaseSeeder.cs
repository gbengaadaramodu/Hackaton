using Microsoft.EntityFrameworkCore;
using SimuLearn.Application.Abstractions;
using SimuLearn.Application.Common;
using SimuLearn.Application.Simulation;
using SimuLearn.Domain.Assessments;
using SimuLearn.Domain.Assignments;
using SimuLearn.Domain.Classrooms;
using SimuLearn.Domain.Curriculum;
using SimuLearn.Domain.Identity;
using SimuLearn.Domain.Lessons;
using SimuLearn.Domain.Simulations;
using SimuLearn.Domain.Simulations.Models;

namespace SimuLearn.Infrastructure.Persistence;

/// <summary>
/// Creates the reference curriculum, the two approved physics templates, a published lesson with an
/// assessment, and a demo class. Only runs when the database has no users, so it never overwrites
/// real data.
/// </summary>
public static class DatabaseSeeder
{
    public static async Task<bool> SeedAsync(
        SimuLearnDbContext db,
        IPasswordHasher passwordHasher,
        SeedOptions options,
        CancellationToken cancellationToken)
    {
        if (!options.Enabled)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(options.Password) || options.Password.Length < 10)
        {
            throw new InvalidOperationException(
                "Seed:Password must be configured with at least 10 characters when demo seeding is enabled.");
        }

        if (await db.Users.AnyAsync(cancellationToken))
        {
            return false;
        }

        var now = DateTimeOffset.UtcNow;

        var institution = new Institution
        {
            Name = options.InstitutionName,
            CountryCode = "NG",
        };
        db.Institutions.Add(institution);

        var admin = CreateUser(options.AdminEmail, "Demo Administrator", RoleName.Administrator, institution, passwordHasher, options.Password);
        var teacher = CreateUser(options.TeacherEmail, "Ada Teacher", RoleName.Teacher, institution, passwordHasher, options.Password);
        var reviewer = CreateUser(options.ReviewerEmail, "Tunde Reviewer", RoleName.ContentReviewer, institution, passwordHasher, options.Password);
        var student = CreateUser(options.StudentEmail, "Chidi Student", RoleName.Student, institution, passwordHasher, options.Password);
        var secondStudent = CreateUser(
            "student2@simulearn.demo",
            "Ngozi Student",
            RoleName.Student,
            institution,
            passwordHasher,
            options.Password);

        db.Users.AddRange(admin, teacher, reviewer, student, secondStudent);

        var physics = new Subject { Name = "Physics", Code = "PHY" };
        var history = new Subject { Name = "History", Code = "HIS" };
        db.Subjects.AddRange(physics, history);

        var standard = new CurriculumStandard
        {
            Region = "NG",
            CurriculumCode = "NG-SS-PHY",
            Version = "2024",
            Title = "Nigerian Senior Secondary Physics (reference)",
        };
        var historyStandard = new CurriculumStandard
        {
            Region = "NG",
            CurriculumCode = "NG-SS-HIS",
            Version = "2024",
            Title = "Nigerian Senior Secondary History (reference)",
        };
        db.CurriculumStandards.AddRange(standard, historyStandard);

        var inclineObjective = new LearningObjective
        {
            Code = "PHY-MECH-INCLINE",
            Description = "Analyse motion on an inclined plane using components of weight and friction.",
            Level = "SS2",
            Subject = physics,
            CurriculumStandard = standard,
        };

        var circuitObjective = new LearningObjective
        {
            Code = "PHY-ELEC-OHMS-LAW",
            Description = "Apply Ohm's law and the power relationship to a simple DC circuit.",
            Level = "SS2",
            Subject = physics,
            CurriculumStandard = standard,
        };

        var independenceObjective = new LearningObjective
        {
            Code = "HIST-NA-INDEPENDENCE",
            Description = "Explain how literacy, the press and nationalist organisation built momentum for Nigerian independence.",
            Level = "SS2",
            Subject = history,
            CurriculumStandard = historyStandard,
        };

        db.LearningObjectives.AddRange(inclineObjective, circuitObjective, independenceObjective);

        db.SimulationTemplates.AddRange(
            new SimulationTemplate
            {
                ModelKey = InclinedPlaneModel.Key,
                Title = "Block on an inclined plane",
                Description = "Slide a block down a ramp and explore angle, friction, mass and ramp length.",
                Domain = "Mechanics",
                SchemaVersion = "1.0.0",
                SubjectId = physics.Id,
                CurriculumStandardId = standard.Id,
                ApprovedAt = now,
            },
            new SimulationTemplate
            {
                ModelKey = DcCircuitModel.Key,
                Title = "Current in a simple DC circuit",
                Description = "Change voltage and resistance and observe current and power.",
                Domain = "Electricity and magnetism",
                SchemaVersion = "1.0.0",
                SubjectId = physics.Id,
                CurriculumStandardId = standard.Id,
                ApprovedAt = now,
            },
            new SimulationTemplate
            {
                ModelKey = NigerianIndependenceModel.Key,
                Title = "The road to Nigerian independence",
                Description = "Explore how literacy, newspapers and nationalist parties built momentum toward independence in 1960.",
                Domain = "Nigerian nationalism",
                SchemaVersion = "1.0.0",
                SubjectId = history.Id,
                CurriculumStandardId = historyStandard.Id,
                ApprovedAt = now,
            });

        var inclineState = new Dictionary<string, double>
        {
            ["massKg"] = 2,
            ["angleDegrees"] = 20,
            ["frictionCoefficient"] = 0.15,
            ["rampLengthM"] = 4,
        };

        var circuitState = new Dictionary<string, double>
        {
            ["voltageV"] = 12,
            ["resistanceOhm"] = 6,
        };

        var independenceState = new Dictionary<string, double>
        {
            ["focusYear"] = 1950,
            ["literacyPercent"] = 35,
            ["pressFreedomIndex"] = 55,
            ["nationalistMobilisation"] = 60,
            ["colonialRestrictiveness"] = 40,
        };

        var inclineLesson = new Lesson
        {
            Title = "Why does a steeper ramp make the block speed up?",
            Description = "A guided exploration of the forces on a block sliding down an inclined plane.",
            Subject = physics,
            AuthorId = teacher.Id,
            GradeLevel = "SS2",
            ModelKey = InclinedPlaneModel.Key,
            Status = LessonStatus.Published,
            DraftConfigurationJson = JsonDefaults.Serialize(LessonConfiguration.FromState(inclineState)),
            PublishedConfigurationJson = JsonDefaults.Serialize(LessonConfiguration.FromState(inclineState)),
            DraftVersion = 1,
            PublishedVersion = 1,
            PublishedAt = now,
            PublishedByUserId = teacher.Id,
            Objectives = [new LessonObjective { LearningObjectiveId = inclineObjective.Id }],
        };

        var circuitLesson = new Lesson
        {
            Title = "Ohm's law in one loop",
            Description = "Change the voltage and resistance of a simple circuit and explain the current you observe.",
            Subject = physics,
            AuthorId = teacher.Id,
            GradeLevel = "SS2",
            ModelKey = DcCircuitModel.Key,
            Status = LessonStatus.Published,
            DraftConfigurationJson = JsonDefaults.Serialize(LessonConfiguration.FromState(circuitState)),
            PublishedConfigurationJson = JsonDefaults.Serialize(LessonConfiguration.FromState(circuitState)),
            DraftVersion = 1,
            PublishedVersion = 1,
            PublishedAt = now,
            PublishedByUserId = teacher.Id,
            Objectives = [new LessonObjective { LearningObjectiveId = circuitObjective.Id }],
        };

        var independenceLesson = new Lesson
        {
            Title = "What built momentum toward independence by 1960?",
            Description = "Move the focus year and change literacy, press freedom and mobilisation to see how momentum grew.",
            Subject = history,
            AuthorId = teacher.Id,
            GradeLevel = "SS2",
            ModelKey = NigerianIndependenceModel.Key,
            Status = LessonStatus.Published,
            DraftConfigurationJson = JsonDefaults.Serialize(LessonConfiguration.FromState(independenceState)),
            PublishedConfigurationJson = JsonDefaults.Serialize(LessonConfiguration.FromState(independenceState)),
            DraftVersion = 1,
            PublishedVersion = 1,
            PublishedAt = now,
            PublishedByUserId = teacher.Id,
            Objectives = [new LessonObjective { LearningObjectiveId = independenceObjective.Id }],
        };

        db.Lessons.AddRange(inclineLesson, circuitLesson, independenceLesson);

        db.Assessments.AddRange(
            new Assessment
            {
                Title = "Forces on an inclined plane",
                LessonId = inclineLesson.Id,
                ModelKey = InclinedPlaneModel.Key,
                Status = AssessmentStatus.Published,
                Version = 1,
                PublishedAt = now,
                QuestionsJson = JsonDefaults.Serialize(InclineQuestions()),
            },
            new Assessment
            {
                Title = "Ohm's law check",
                LessonId = circuitLesson.Id,
                ModelKey = DcCircuitModel.Key,
                Status = AssessmentStatus.Published,
                Version = 1,
                PublishedAt = now,
                QuestionsJson = JsonDefaults.Serialize(CircuitQuestions()),
            },
            new Assessment
            {
                Title = "Road to independence check",
                LessonId = independenceLesson.Id,
                ModelKey = NigerianIndependenceModel.Key,
                Status = AssessmentStatus.Published,
                Version = 1,
                PublishedAt = now,
                QuestionsJson = JsonDefaults.Serialize(IndependenceQuestions()),
            });

        var classRoom = new ClassRoom
        {
            Name = "SS2 Physics A",
            TeacherId = teacher.Id,
            Institution = institution,
            JoinCode = "PHY202",
        };

        db.ClassRooms.Add(classRoom);

        db.ClassMembers.AddRange(
            new ClassMember { ClassRoomId = classRoom.Id, StudentId = student.Id },
            new ClassMember { ClassRoomId = classRoom.Id, StudentId = secondStudent.Id });

        db.Assignments.Add(new Assignment
        {
            ClassRoomId = classRoom.Id,
            LessonId = inclineLesson.Id,
            TeacherId = teacher.Id,
            Instructions = "Explore three ramp angles, then answer the quiz and explain the pattern you found.",
            DueAt = now.AddDays(14),
        });

        await db.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static User CreateUser(
        string email,
        string displayName,
        RoleName role,
        Institution institution,
        IPasswordHasher passwordHasher,
        string password) =>
        new()
        {
            Email = email,
            NormalizedEmail = email.Trim().ToLowerInvariant(),
            DisplayName = displayName,
            PasswordHash = passwordHasher.Hash(password),
            Institution = institution,
            Roles = [new UserRole { Role = role }],
        };

    private static List<AssessmentQuestion> InclineQuestions() =>
    [
        new()
        {
            Id = "incline-1",
            Prompt = "A block slides down a ramp at angle θ with kinetic friction coefficient μk. " +
                     "Which expression gives its acceleration along the plane?",
            Type = QuestionType.SingleChoice,
            Options =
            [
                new("a", "g(sin θ − μk cos θ)"),
                new("b", "g(sin θ + μk cos θ)"),
                new("c", "g cos θ"),
                new("d", "g sin θ + μk g"),
            ],
            CorrectOptionIds = ["a"],
            Explanation = "The driving component is mg sin θ and kinetic friction is μk mg cos θ. Dividing the net " +
                          "force by mass gives a = g(sin θ − μk cos θ).",
            LearningObjectiveCode = "PHY-MECH-INCLINE",
        },
        new()
        {
            Id = "incline-2",
            Prompt = "A ramp is at 30° with μk = 0.15. Taking g = 9.81 m/s², what is the block's acceleration in m/s²? " +
                     "Give your answer to two decimal places.",
            Type = QuestionType.Numeric,
            NumericAnswer = 3.63,
            NumericTolerance = 0.05,
            Explanation = "a = 9.81 × (sin 30° − 0.15 cos 30°) = 9.81 × (0.5 − 0.1299) ≈ 3.63 m/s².",
            LearningObjectiveCode = "PHY-MECH-INCLINE",
        },
        new()
        {
            Id = "incline-3",
            Prompt = "Keeping the angle and friction coefficient fixed, what happens to the acceleration when the " +
                     "block's mass is doubled?",
            Type = QuestionType.SingleChoice,
            Options =
            [
                new("a", "It doubles"),
                new("b", "It halves"),
                new("c", "It stays the same"),
                new("d", "It becomes zero"),
            ],
            CorrectOptionIds = ["c"],
            Explanation = "Both the driving component and friction scale with mass, so mass cancels out of " +
                          "a = g(sin θ − μk cos θ).",
            LearningObjectiveCode = "PHY-MECH-INCLINE",
        },
        new()
        {
            Id = "incline-4",
            Prompt = "In this simplified model, what is the normal force on the block?",
            Type = QuestionType.SingleChoice,
            Options =
            [
                new("a", "mg sin θ"),
                new("b", "mg cos θ"),
                new("c", "mg"),
                new("d", "μk mg"),
            ],
            CorrectOptionIds = ["b"],
            Explanation = "The surface supports the component of weight perpendicular to the plane, which is mg cos θ.",
            LearningObjectiveCode = "PHY-MECH-INCLINE",
        },
    ];

    private static List<AssessmentQuestion> CircuitQuestions() =>
    [
        new()
        {
            Id = "circuit-1",
            Prompt = "Which relationship correctly gives the current through an ohmic resistor?",
            Type = QuestionType.SingleChoice,
            Options =
            [
                new("a", "I = V / R"),
                new("b", "I = V × R"),
                new("c", "I = R / V"),
                new("d", "I = V + R"),
            ],
            CorrectOptionIds = ["a"],
            Explanation = "Ohm's law states that current is proportional to voltage and inversely proportional to resistance.",
            LearningObjectiveCode = "PHY-ELEC-OHMS-LAW",
        },
        new()
        {
            Id = "circuit-2",
            Prompt = "A 12 V source is connected across a 6 Ω resistor. What current flows, in amperes?",
            Type = QuestionType.Numeric,
            NumericAnswer = 2,
            NumericTolerance = 0.01,
            Explanation = "I = V / R = 12 / 6 = 2 A.",
            LearningObjectiveCode = "PHY-ELEC-OHMS-LAW",
        },
        new()
        {
            Id = "circuit-3",
            Prompt = "At constant voltage, what happens to the current if the resistance is doubled?",
            Type = QuestionType.SingleChoice,
            Options =
            [
                new("a", "It doubles"),
                new("b", "It halves"),
                new("c", "It stays the same"),
                new("d", "It becomes zero"),
            ],
            CorrectOptionIds = ["b"],
            Explanation = "Current is inversely proportional to resistance, so doubling R halves I.",
            LearningObjectiveCode = "PHY-ELEC-OHMS-LAW",
        },
    ];

    private static List<AssessmentQuestion> IndependenceQuestions() =>
    [
        new()
        {
            Id = "independence-1",
            Prompt = "In the independence simulation, what happens to movement momentum when both press freedom and mobilisation rise?",
            Type = QuestionType.SingleChoice,
            Options =
            [
                new("a", "It rises, because momentum multiplies the two"),
                new("b", "It stays the same"),
                new("c", "It falls to zero"),
                new("d", "It becomes negative"),
            ],
            CorrectOptionIds = ["a"],
            Explanation = "Momentum is mobilisation multiplied by press freedom, so raising both raises momentum.",
            LearningObjectiveCode = "HIST-NA-INDEPENDENCE",
        },
        new()
        {
            Id = "independence-2",
            Prompt = "Why does higher literacy raise the readiness index in the simulation?",
            Type = QuestionType.SingleChoice,
            Options =
            [
                new("a", "More readers could follow newspapers and party messages"),
                new("b", "It lowers colonial restrictiveness automatically"),
                new("c", "It changes the focus year"),
                new("d", "It removes the need for parties"),
            ],
            CorrectOptionIds = ["a"],
            Explanation = "Literacy lets more people read nationalist newspapers and pamphlets.",
            LearningObjectiveCode = "HIST-NA-INDEPENDENCE",
        },
        new()
        {
            Id = "independence-3",
            Prompt = "Nigeria became independent in which year?",
            Type = QuestionType.SingleChoice,
            Options =
            [
                new("a", "1947"),
                new("b", "1957"),
                new("c", "1960"),
                new("d", "1963"),
            ],
            CorrectOptionIds = ["c"],
            Explanation = "Nigeria became independent on 1 October 1960.",
            LearningObjectiveCode = "HIST-NA-INDEPENDENCE",
        },
    ];
}
