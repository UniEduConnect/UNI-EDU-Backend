CREATE TABLE IF NOT EXISTS "__EFMigrationsHistory" (
    "MigrationId" character varying(150) NOT NULL,
    "ProductVersion" character varying(32) NOT NULL,
    CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY ("MigrationId")
);

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260514050410_AddUserTable') THEN
    CREATE TABLE "Users" (
        "Id" uuid NOT NULL,
        "LastName" text NOT NULL,
        "FirstName" text NOT NULL,
        CONSTRAINT "PK_Users" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260514050410_AddUserTable') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260514050410_AddUserTable', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    ALTER TABLE "Users" RENAME COLUMN "LastName" TO "PhoneNumber";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    ALTER TABLE "Users" RENAME COLUMN "FirstName" TO "HashedPassword";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    ALTER TABLE "Users" RENAME COLUMN "Id" TO "UserID";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    ALTER TABLE "Users" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    ALTER TABLE "Users" ADD "Email" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    ALTER TABLE "Users" ADD "Fullname" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    ALTER TABLE "Users" ADD "Role" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "Parents" (
        "ParentID" uuid NOT NULL,
        "FullName" text NOT NULL,
        CONSTRAINT "PK_Parents" PRIMARY KEY ("ParentID"),
        CONSTRAINT "FK_Parents_Users_ParentID" FOREIGN KEY ("ParentID") REFERENCES "Users" ("UserID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "RefreshTokens" (
        "TokenID" uuid NOT NULL,
        "UserID" uuid NOT NULL,
        "Token" text NOT NULL,
        "JwtId" text NOT NULL,
        "IsUsed" boolean NOT NULL,
        "IsRevoked" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_RefreshTokens" PRIMARY KEY ("TokenID"),
        CONSTRAINT "FK_RefreshTokens_Users_UserID" FOREIGN KEY ("UserID") REFERENCES "Users" ("UserID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "Subjects" (
        "SubjectID" uuid NOT NULL,
        "SubjectName" text NOT NULL,
        CONSTRAINT "PK_Subjects" PRIMARY KEY ("SubjectID")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "Tutors" (
        "TutorID" uuid NOT NULL,
        "FullName" text NOT NULL,
        "DateOfBirth" timestamp with time zone NOT NULL,
        "Gender" text NOT NULL,
        "Address" text NOT NULL,
        "Degree" text NOT NULL,
        "Experience" text NOT NULL,
        "Bio" text NOT NULL,
        "AverageRating" real NOT NULL,
        CONSTRAINT "PK_Tutors" PRIMARY KEY ("TutorID"),
        CONSTRAINT "FK_Tutors_Users_TutorID" FOREIGN KEY ("TutorID") REFERENCES "Users" ("UserID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "Students" (
        "StudentID" uuid NOT NULL,
        "ParentID" uuid,
        "FullName" text NOT NULL,
        "School" text NOT NULL,
        "Grade" integer NOT NULL,
        CONSTRAINT "PK_Students" PRIMARY KEY ("StudentID"),
        CONSTRAINT "FK_Students_Parents_ParentID" FOREIGN KEY ("ParentID") REFERENCES "Parents" ("ParentID"),
        CONSTRAINT "FK_Students_Users_StudentID" FOREIGN KEY ("StudentID") REFERENCES "Users" ("UserID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "Questions" (
        "QuestionID" integer GENERATED BY DEFAULT AS IDENTITY,
        "SubjectID" uuid NOT NULL,
        "Content" text NOT NULL,
        "Type" integer NOT NULL,
        "Difficulty" integer NOT NULL,
        "OptionA" text NOT NULL,
        "OptionB" text NOT NULL,
        "OptionC" text NOT NULL,
        "OptionD" text NOT NULL,
        "CorrectAnswer" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Questions" PRIMARY KEY ("QuestionID"),
        CONSTRAINT "FK_Questions_Subjects_SubjectID" FOREIGN KEY ("SubjectID") REFERENCES "Subjects" ("SubjectID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "Exams" (
        "ExamID" integer GENERATED BY DEFAULT AS IDENTITY,
        "SubjectID" uuid NOT NULL,
        "Title" text NOT NULL,
        "Description" text NOT NULL,
        "Duration" integer NOT NULL,
        "Type" integer NOT NULL,
        "CreatedBy" integer NOT NULL,
        "CreatorTutorID" uuid,
        CONSTRAINT "PK_Exams" PRIMARY KEY ("ExamID"),
        CONSTRAINT "FK_Exams_Subjects_SubjectID" FOREIGN KEY ("SubjectID") REFERENCES "Subjects" ("SubjectID") ON DELETE CASCADE,
        CONSTRAINT "FK_Exams_Tutors_CreatorTutorID" FOREIGN KEY ("CreatorTutorID") REFERENCES "Tutors" ("TutorID")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "ClassSessions" (
        "ClassID" uuid NOT NULL,
        "TutorID" uuid NOT NULL,
        "StudentID" uuid NOT NULL,
        "SubjectID" uuid NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "TuitionFee" numeric NOT NULL,
        "Status" integer NOT NULL,
        CONSTRAINT "PK_ClassSessions" PRIMARY KEY ("ClassID"),
        CONSTRAINT "FK_ClassSessions_Students_StudentID" FOREIGN KEY ("StudentID") REFERENCES "Students" ("StudentID") ON DELETE RESTRICT,
        CONSTRAINT "FK_ClassSessions_Subjects_SubjectID" FOREIGN KEY ("SubjectID") REFERENCES "Subjects" ("SubjectID") ON DELETE CASCADE,
        CONSTRAINT "FK_ClassSessions_Tutors_TutorID" FOREIGN KEY ("TutorID") REFERENCES "Tutors" ("TutorID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "ExamQuestions" (
        "ExamID" integer NOT NULL,
        "QuestionID" integer NOT NULL,
        "QuestionOrder" integer NOT NULL,
        CONSTRAINT "PK_ExamQuestions" PRIMARY KEY ("ExamID", "QuestionID"),
        CONSTRAINT "FK_ExamQuestions_Exams_ExamID" FOREIGN KEY ("ExamID") REFERENCES "Exams" ("ExamID") ON DELETE CASCADE,
        CONSTRAINT "FK_ExamQuestions_Questions_QuestionID" FOREIGN KEY ("QuestionID") REFERENCES "Questions" ("QuestionID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "Submissions" (
        "SubmissionID" integer GENERATED BY DEFAULT AS IDENTITY,
        "ExamID" integer NOT NULL,
        "UserID" uuid NOT NULL,
        "SubmissionDate" timestamp with time zone NOT NULL,
        "Answers" text NOT NULL,
        "Score" real NOT NULL,
        "AIFeedback" text NOT NULL,
        CONSTRAINT "PK_Submissions" PRIMARY KEY ("SubmissionID"),
        CONSTRAINT "FK_Submissions_Exams_ExamID" FOREIGN KEY ("ExamID") REFERENCES "Exams" ("ExamID") ON DELETE CASCADE,
        CONSTRAINT "FK_Submissions_Users_UserID" FOREIGN KEY ("UserID") REFERENCES "Users" ("UserID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE TABLE "Reviews" (
        "ReviewID" integer GENERATED BY DEFAULT AS IDENTITY,
        "ReviewerID" uuid NOT NULL,
        "TutorID" uuid NOT NULL,
        "ClassID" uuid NOT NULL,
        "Rating" integer NOT NULL,
        "Comment" text NOT NULL,
        "ReviewDate" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Reviews" PRIMARY KEY ("ReviewID"),
        CONSTRAINT "FK_Reviews_ClassSessions_ClassID" FOREIGN KEY ("ClassID") REFERENCES "ClassSessions" ("ClassID") ON DELETE CASCADE,
        CONSTRAINT "FK_Reviews_Tutors_TutorID" FOREIGN KEY ("TutorID") REFERENCES "Tutors" ("TutorID") ON DELETE RESTRICT,
        CONSTRAINT "FK_Reviews_Users_ReviewerID" FOREIGN KEY ("ReviewerID") REFERENCES "Users" ("UserID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_ClassSessions_StudentID" ON "ClassSessions" ("StudentID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_ClassSessions_SubjectID" ON "ClassSessions" ("SubjectID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_ClassSessions_TutorID" ON "ClassSessions" ("TutorID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_ExamQuestions_QuestionID" ON "ExamQuestions" ("QuestionID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Exams_CreatorTutorID" ON "Exams" ("CreatorTutorID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Exams_SubjectID" ON "Exams" ("SubjectID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Questions_SubjectID" ON "Questions" ("SubjectID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_RefreshTokens_UserID" ON "RefreshTokens" ("UserID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Reviews_ClassID" ON "Reviews" ("ClassID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Reviews_ReviewerID" ON "Reviews" ("ReviewerID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Reviews_TutorID" ON "Reviews" ("TutorID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Students_ParentID" ON "Students" ("ParentID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Submissions_ExamID" ON "Submissions" ("ExamID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    CREATE INDEX "IX_Submissions_UserID" ON "Submissions" ("UserID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260517032544_AddEntities') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260517032544_AddEntities', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260520131623_UpdateTutor') THEN
    ALTER TABLE "Tutors" DROP COLUMN "Bio";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260520131623_UpdateTutor') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "Experience" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260520131623_UpdateTutor') THEN
    ALTER TABLE "Tutors" ADD "StudentIdNumber" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260520131623_UpdateTutor') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260520131623_UpdateTutor', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "Achievements" text[] NOT NULL DEFAULT (ARRAY[]::text[]);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "AvailableSlots" jsonb NOT NULL DEFAULT ('[]'::jsonb);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "AvatarUrl" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "Certificates" text[] NOT NULL DEFAULT (ARRAY[]::text[]);
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "HourlyRate" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "IntroVideoUrl" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "IsVerified" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "Location" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "School" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "TeachingStyle" text NOT NULL DEFAULT '';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "TutorType" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    ALTER TABLE "Tutors" ADD "YearsExperience" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    CREATE TABLE "TutorSubjects" (
        "TutorID" uuid NOT NULL,
        "SubjectID" uuid NOT NULL,
        CONSTRAINT "PK_TutorSubjects" PRIMARY KEY ("TutorID", "SubjectID"),
        CONSTRAINT "FK_TutorSubjects_Subjects_SubjectID" FOREIGN KEY ("SubjectID") REFERENCES "Subjects" ("SubjectID") ON DELETE CASCADE,
        CONSTRAINT "FK_TutorSubjects_Tutors_TutorID" FOREIGN KEY ("TutorID") REFERENCES "Tutors" ("TutorID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    CREATE INDEX "IX_TutorSubjects_SubjectID" ON "TutorSubjects" ("SubjectID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260523083031_AddTutorSubject') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260523083031_AddTutorSubject', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524063903_RefactorTutor') THEN
    ALTER TABLE "Tutors" ADD "Bio" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524063903_RefactorTutor') THEN
    ALTER TABLE "Tutors" DROP COLUMN "Address";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524063903_RefactorTutor') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260524063903_RefactorTutor', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "YearsExperience" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "TutorType" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "TeachingStyle" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "School" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "Location" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "IsVerified" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "IntroVideoUrl" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "HourlyRate" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "DateOfBirth" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "Certificates" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "Bio" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "AverageRating" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "AvatarUrl" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "AvailableSlots" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    ALTER TABLE "Tutors" ALTER COLUMN "Achievements" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524072228_RefactorNullableFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260524072228_RefactorNullableFields', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    ALTER TABLE "Reviews" DROP CONSTRAINT "FK_Reviews_ClassSessions_ClassID";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    DROP TABLE "ClassSessions";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE TABLE "Classes" (
        "ClassID" uuid NOT NULL,
        "TutorID" uuid NOT NULL,
        "StudentID" uuid NOT NULL,
        "SubjectID" uuid NOT NULL,
        "Name" text NOT NULL,
        "StartDate" timestamp with time zone NOT NULL,
        "TuitionFee" numeric NOT NULL,
        "Status" integer NOT NULL,
        "Format" integer NOT NULL,
        "Schedule" text NOT NULL,
        "TotalSessions" integer NOT NULL,
        "CompletedSessions" integer NOT NULL,
        "EscrowAmount" numeric NOT NULL,
        "EscrowReleased" numeric NOT NULL,
        "EscrowStatus" integer NOT NULL,
        "ReleaseMilestone" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Classes" PRIMARY KEY ("ClassID"),
        CONSTRAINT "FK_Classes_Students_StudentID" FOREIGN KEY ("StudentID") REFERENCES "Students" ("StudentID") ON DELETE RESTRICT,
        CONSTRAINT "FK_Classes_Subjects_SubjectID" FOREIGN KEY ("SubjectID") REFERENCES "Subjects" ("SubjectID") ON DELETE CASCADE,
        CONSTRAINT "FK_Classes_Tutors_TutorID" FOREIGN KEY ("TutorID") REFERENCES "Tutors" ("TutorID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE TABLE "Wallets" (
        "UserID" uuid NOT NULL,
        "Balance" numeric NOT NULL,
        "EscrowBalance" numeric NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Wallets" PRIMARY KEY ("UserID"),
        CONSTRAINT "FK_Wallets_Users_UserID" FOREIGN KEY ("UserID") REFERENCES "Users" ("UserID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE TABLE "Sessions" (
        "SessionID" uuid NOT NULL,
        "ClassID" uuid NOT NULL,
        "Date" timestamp with time zone NOT NULL,
        "Time" text NOT NULL,
        "Status" integer NOT NULL,
        "Format" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Sessions" PRIMARY KEY ("SessionID"),
        CONSTRAINT "FK_Sessions_Classes_ClassID" FOREIGN KEY ("ClassID") REFERENCES "Classes" ("ClassID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE TABLE "WalletTransactions" (
        "TransactionID" uuid NOT NULL,
        "UserID" uuid NOT NULL,
        "Type" integer NOT NULL,
        "Amount" numeric NOT NULL,
        "RelatedClassID" uuid,
        "Description" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_WalletTransactions" PRIMARY KEY ("TransactionID"),
        CONSTRAINT "FK_WalletTransactions_Classes_RelatedClassID" FOREIGN KEY ("RelatedClassID") REFERENCES "Classes" ("ClassID") ON DELETE SET NULL,
        CONSTRAINT "FK_WalletTransactions_Wallets_UserID" FOREIGN KEY ("UserID") REFERENCES "Wallets" ("UserID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE INDEX "IX_Classes_StudentID" ON "Classes" ("StudentID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE INDEX "IX_Classes_SubjectID" ON "Classes" ("SubjectID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE INDEX "IX_Classes_TutorID" ON "Classes" ("TutorID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE INDEX "IX_Sessions_ClassID" ON "Sessions" ("ClassID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE INDEX "IX_WalletTransactions_RelatedClassID" ON "WalletTransactions" ("RelatedClassID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    CREATE INDEX "IX_WalletTransactions_UserID" ON "WalletTransactions" ("UserID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    ALTER TABLE "Reviews" ADD CONSTRAINT "FK_Reviews_Classes_ClassID" FOREIGN KEY ("ClassID") REFERENCES "Classes" ("ClassID") ON DELETE CASCADE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260524073519_UpdateClassFromClassSession_AddSession_AddWallet_AddWalletTransaction', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524090713_AddClassScheduleSlot') THEN
    ALTER TABLE "Sessions" DROP COLUMN "Time";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524090713_AddClassScheduleSlot') THEN
    ALTER TABLE "Classes" DROP COLUMN "Schedule";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524090713_AddClassScheduleSlot') THEN
    ALTER TABLE "Sessions" RENAME COLUMN "Date" TO "StartAt";
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524090713_AddClassScheduleSlot') THEN
    ALTER TABLE "Sessions" ADD "EndAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524090713_AddClassScheduleSlot') THEN
    ALTER TABLE "Classes" ADD "WeeklySlots" jsonb NOT NULL DEFAULT '{}';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260524090713_AddClassScheduleSlot') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260524090713_AddClassScheduleSlot', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525072233_AddClassMaterial') THEN
    CREATE TABLE "ClassMaterials" (
        "MaterialID" uuid NOT NULL,
        "ClassID" uuid NOT NULL,
        "Name" text NOT NULL,
        "Type" text NOT NULL,
        "Url" text NOT NULL,
        "Size" text,
        "UploadedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ClassMaterials" PRIMARY KEY ("MaterialID"),
        CONSTRAINT "FK_ClassMaterials_Classes_ClassID" FOREIGN KEY ("ClassID") REFERENCES "Classes" ("ClassID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525072233_AddClassMaterial') THEN
    CREATE INDEX "IX_ClassMaterials_ClassID" ON "ClassMaterials" ("ClassID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260525072233_AddClassMaterial') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260525072233_AddClassMaterial', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260529074227_AddWalletTxStatusAndPayment') THEN
    ALTER TABLE "WalletTransactions" ADD "Method" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260529074227_AddWalletTxStatusAndPayment') THEN
    ALTER TABLE "WalletTransactions" ADD "ProviderRef" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260529074227_AddWalletTxStatusAndPayment') THEN
    ALTER TABLE "WalletTransactions" ADD "ProviderTxnId" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260529074227_AddWalletTxStatusAndPayment') THEN
    ALTER TABLE "WalletTransactions" ADD "Status" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260529074227_AddWalletTxStatusAndPayment') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260529074227_AddWalletTxStatusAndPayment', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260530162940_AddWithdrawals') THEN
    CREATE TABLE "Withdrawals" (
        "WithdrawalID" uuid NOT NULL,
        "TutorID" uuid NOT NULL,
        "Amount" numeric NOT NULL,
        "Method" text NOT NULL,
        "BankName" text,
        "BankAccount" text,
        "Note" text,
        "Status" integer NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "ReviewedAt" timestamp with time zone,
        "ReviewerID" uuid,
        "ReviewNote" text,
        CONSTRAINT "PK_Withdrawals" PRIMARY KEY ("WithdrawalID"),
        CONSTRAINT "FK_Withdrawals_Tutors_TutorID" FOREIGN KEY ("TutorID") REFERENCES "Tutors" ("TutorID") ON DELETE RESTRICT,
        CONSTRAINT "FK_Withdrawals_Users_ReviewerID" FOREIGN KEY ("ReviewerID") REFERENCES "Users" ("UserID") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260530162940_AddWithdrawals') THEN
    CREATE INDEX "IX_Withdrawals_ReviewerID" ON "Withdrawals" ("ReviewerID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260530162940_AddWithdrawals') THEN
    CREATE INDEX "IX_Withdrawals_TutorID" ON "Withdrawals" ("TutorID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260530162940_AddWithdrawals') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260530162940_AddWithdrawals', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260530170756_AddTutorBankAccount') THEN
    ALTER TABLE "Tutors" ADD "BankAccount" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260530170756_AddTutorBankAccount') THEN
    ALTER TABLE "Tutors" ADD "BankAccountHolder" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260530170756_AddTutorBankAccount') THEN
    ALTER TABLE "Tutors" ADD "BankName" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260530170756_AddTutorBankAccount') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260530170756_AddTutorBankAccount', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "AbsenceApproved" boolean;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "AbsenceReason" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "AbsenceRequestedBy" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "Content" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "EndedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "Homework" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "HomeworkFiles" text[];
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "Notes" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "Rating" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "RatingComment" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    ALTER TABLE "Sessions" ADD "StartedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260531125059_AddSessionDetails') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260531125059_AddSessionDetails', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Submissions" ALTER COLUMN "AIFeedback" DROP NOT NULL;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Submissions" ADD "CorrectCount" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Submissions" ADD "TotalQuestions" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Questions" ADD "Standard" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Questions" ADD "Topic" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "AiProctoring" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "CreatedAt" timestamp with time zone NOT NULL DEFAULT TIMESTAMPTZ '-infinity';
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "Difficulty" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "EndDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "Fee" numeric NOT NULL DEFAULT 0.0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "MaxAttemptsPerUser" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "ScoreScale" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "StartDate" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "Status" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    ALTER TABLE "Exams" ADD "Year" integer NOT NULL DEFAULT 0;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602183841_AddExamConfigQuestionMetaAndSubmissionScores') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260602183841_AddExamConfigQuestionMetaAndSubmissionScores', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602185426_AddUserStatusAndAuditLog') THEN
    ALTER TABLE "Users" ADD "Status" integer NOT NULL DEFAULT 1;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602185426_AddUserStatusAndAuditLog') THEN
    CREATE TABLE "AuditLogs" (
        "AuditLogID" uuid NOT NULL,
        "ActorUserId" uuid,
        "ActorName" text NOT NULL,
        "Action" text NOT NULL,
        "Target" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_AuditLogs" PRIMARY KEY ("AuditLogID"),
        CONSTRAINT "FK_AuditLogs_Users_ActorUserId" FOREIGN KEY ("ActorUserId") REFERENCES "Users" ("UserID") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602185426_AddUserStatusAndAuditLog') THEN
    CREATE INDEX "IX_AuditLogs_ActorUserId" ON "AuditLogs" ("ActorUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602185426_AddUserStatusAndAuditLog') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260602185426_AddUserStatusAndAuditLog', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602185848_AddChatMessages') THEN
    CREATE TABLE "Messages" (
        "MessageID" uuid NOT NULL,
        "ClassID" uuid NOT NULL,
        "SenderID" uuid NOT NULL,
        "SenderRole" text NOT NULL,
        "Content" text NOT NULL,
        "IsRead" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Messages" PRIMARY KEY ("MessageID"),
        CONSTRAINT "FK_Messages_Classes_ClassID" FOREIGN KEY ("ClassID") REFERENCES "Classes" ("ClassID") ON DELETE CASCADE,
        CONSTRAINT "FK_Messages_Users_SenderID" FOREIGN KEY ("SenderID") REFERENCES "Users" ("UserID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602185848_AddChatMessages') THEN
    CREATE INDEX "IX_Messages_ClassID" ON "Messages" ("ClassID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602185848_AddChatMessages') THEN
    CREATE INDEX "IX_Messages_SenderID" ON "Messages" ("SenderID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602185848_AddChatMessages') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260602185848_AddChatMessages', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602190947_AddOfficeAttendanceAndIncidents') THEN
    ALTER TABLE "Sessions" ADD "OfficeConfirmed" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602190947_AddOfficeAttendanceAndIncidents') THEN
    CREATE TABLE "Incidents" (
        "IncidentID" uuid NOT NULL,
        "ClassID" uuid NOT NULL,
        "SessionID" uuid,
        "ReporterUserId" uuid,
        "ReporterName" text NOT NULL,
        "ReporterRole" text NOT NULL,
        "Description" text NOT NULL,
        "Priority" integer NOT NULL,
        "Status" integer NOT NULL,
        "Resolution" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ResolvedAt" timestamp with time zone,
        CONSTRAINT "PK_Incidents" PRIMARY KEY ("IncidentID"),
        CONSTRAINT "FK_Incidents_Classes_ClassID" FOREIGN KEY ("ClassID") REFERENCES "Classes" ("ClassID") ON DELETE CASCADE,
        CONSTRAINT "FK_Incidents_Sessions_SessionID" FOREIGN KEY ("SessionID") REFERENCES "Sessions" ("SessionID") ON DELETE SET NULL,
        CONSTRAINT "FK_Incidents_Users_ReporterUserId" FOREIGN KEY ("ReporterUserId") REFERENCES "Users" ("UserID") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602190947_AddOfficeAttendanceAndIncidents') THEN
    CREATE INDEX "IX_Incidents_ClassID" ON "Incidents" ("ClassID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602190947_AddOfficeAttendanceAndIncidents') THEN
    CREATE INDEX "IX_Incidents_ReporterUserId" ON "Incidents" ("ReporterUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602190947_AddOfficeAttendanceAndIncidents') THEN
    CREATE INDEX "IX_Incidents_SessionID" ON "Incidents" ("SessionID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260602190947_AddOfficeAttendanceAndIncidents') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260602190947_AddOfficeAttendanceAndIncidents', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603100000_AddTrialBookings') THEN
    CREATE TABLE "TrialBookings" (
        "TrialID" uuid NOT NULL,
        "TutorID" uuid NOT NULL,
        "StudentID" uuid NOT NULL,
        "ParentID" uuid,
        "SubjectID" uuid NOT NULL,
        "RequestedAt" timestamp with time zone NOT NULL,
        "Goals" text,
        "CurrentLevel" text,
        "Note" text,
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TrialBookings" PRIMARY KEY ("TrialID"),
        CONSTRAINT "FK_TrialBookings_Parents_ParentID" FOREIGN KEY ("ParentID") REFERENCES "Parents" ("ParentID") ON DELETE SET NULL,
        CONSTRAINT "FK_TrialBookings_Students_StudentID" FOREIGN KEY ("StudentID") REFERENCES "Students" ("StudentID") ON DELETE RESTRICT,
        CONSTRAINT "FK_TrialBookings_Subjects_SubjectID" FOREIGN KEY ("SubjectID") REFERENCES "Subjects" ("SubjectID") ON DELETE RESTRICT,
        CONSTRAINT "FK_TrialBookings_Tutors_TutorID" FOREIGN KEY ("TutorID") REFERENCES "Tutors" ("TutorID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603100000_AddTrialBookings') THEN
    CREATE INDEX "IX_TrialBookings_ParentID" ON "TrialBookings" ("ParentID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603100000_AddTrialBookings') THEN
    CREATE INDEX "IX_TrialBookings_StudentID" ON "TrialBookings" ("StudentID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603100000_AddTrialBookings') THEN
    CREATE INDEX "IX_TrialBookings_SubjectID" ON "TrialBookings" ("SubjectID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603100000_AddTrialBookings') THEN
    CREATE INDEX "IX_TrialBookings_TutorID" ON "TrialBookings" ("TutorID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603100000_AddTrialBookings') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260603100000_AddTrialBookings', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603110000_AddTrialReviewFields') THEN
    ALTER TABLE "TrialBookings" ADD "ReviewedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603110000_AddTrialReviewFields') THEN
    ALTER TABLE "TrialBookings" ADD "ReviewNote" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603110000_AddTrialReviewFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260603110000_AddTrialReviewFields', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603120000_AddTrialCompletionFields') THEN
    ALTER TABLE "TrialBookings" ADD "CompletedAt" timestamp with time zone;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603120000_AddTrialCompletionFields') THEN
    ALTER TABLE "TrialBookings" ADD "Feedback" text;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603120000_AddTrialCompletionFields') THEN
    ALTER TABLE "TrialBookings" ADD "Rating" double precision;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603120000_AddTrialCompletionFields') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260603120000_AddTrialCompletionFields', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603160219_AddChat') THEN
    CREATE TABLE "ChatMessages" (
        "MessageID" uuid NOT NULL,
        "ClassID" uuid NOT NULL,
        "SenderID" uuid NOT NULL,
        "Message" text NOT NULL,
        "SentAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ChatMessages" PRIMARY KEY ("MessageID"),
        CONSTRAINT "FK_ChatMessages_Classes_ClassID" FOREIGN KEY ("ClassID") REFERENCES "Classes" ("ClassID") ON DELETE CASCADE,
        CONSTRAINT "FK_ChatMessages_Users_SenderID" FOREIGN KEY ("SenderID") REFERENCES "Users" ("UserID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603160219_AddChat') THEN
    CREATE TABLE "ClassChatReads" (
        "ClassID" uuid NOT NULL,
        "UserID" uuid NOT NULL,
        "LastReadAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ClassChatReads" PRIMARY KEY ("ClassID", "UserID")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603160219_AddChat') THEN
    CREATE TABLE "DmMessages" (
        "MessageID" uuid NOT NULL,
        "ParentID" uuid NOT NULL,
        "TutorID" uuid NOT NULL,
        "SenderID" uuid NOT NULL,
        "Message" text NOT NULL,
        "SentAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_DmMessages" PRIMARY KEY ("MessageID")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603160219_AddChat') THEN
    CREATE INDEX "IX_ChatMessages_ClassID_SentAt" ON "ChatMessages" ("ClassID", "SentAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603160219_AddChat') THEN
    CREATE INDEX "IX_ChatMessages_SenderID" ON "ChatMessages" ("SenderID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603160219_AddChat') THEN
    CREATE INDEX "IX_DmMessages_TutorID_ParentID_SentAt" ON "DmMessages" ("TutorID", "ParentID", "SentAt");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260603160219_AddChat') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260603160219_AddChat', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604170513_AddNotificationsAndSettings') THEN
    CREATE TABLE "Notifications" (
        "NotificationID" uuid NOT NULL,
        "UserID" uuid NOT NULL,
        "Title" text NOT NULL,
        "Message" text NOT NULL,
        "Type" text NOT NULL,
        "Link" text,
        "IsRead" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Notifications" PRIMARY KEY ("NotificationID"),
        CONSTRAINT "FK_Notifications_Users_UserID" FOREIGN KEY ("UserID") REFERENCES "Users" ("UserID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604170513_AddNotificationsAndSettings') THEN
    CREATE TABLE "SystemSettings" (
        "SettingID" uuid NOT NULL,
        "PlatformName" text NOT NULL,
        "EscrowPercent" integer NOT NULL,
        "EscrowHoldDays" integer NOT NULL,
        "EnableExams" boolean NOT NULL,
        "EnableChat" boolean NOT NULL,
        "EnablePayments" boolean NOT NULL,
        "MaintenanceMode" boolean NOT NULL,
        "EmailNotifications" boolean NOT NULL,
        "SmsNotifications" boolean NOT NULL,
        "PushNotifications" boolean NOT NULL,
        "TwoFactorAuth" boolean NOT NULL,
        "SessionTimeout" integer NOT NULL,
        "MaxLoginAttempts" integer NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_SystemSettings" PRIMARY KEY ("SettingID")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604170513_AddNotificationsAndSettings') THEN
    CREATE INDEX "IX_Notifications_UserID" ON "Notifications" ("UserID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604170513_AddNotificationsAndSettings') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260604170513_AddNotificationsAndSettings', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604173831_AddRefundsStudentAvailabilityReviewHidden') THEN
    ALTER TABLE "Students" ADD "AvailableSlots" jsonb;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604173831_AddRefundsStudentAvailabilityReviewHidden') THEN
    ALTER TABLE "Reviews" ADD "IsHidden" boolean NOT NULL DEFAULT FALSE;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604173831_AddRefundsStudentAvailabilityReviewHidden') THEN
    CREATE TABLE "RefundRequests" (
        "RefundRequestID" uuid NOT NULL,
        "ClassID" uuid NOT NULL,
        "RequesterUserId" uuid,
        "Amount" numeric NOT NULL,
        "MaxAmount" numeric NOT NULL,
        "Reason" text NOT NULL,
        "Status" integer NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "ReviewedAt" timestamp with time zone,
        "ReviewerID" uuid,
        "ReviewNote" text,
        CONSTRAINT "PK_RefundRequests" PRIMARY KEY ("RefundRequestID"),
        CONSTRAINT "FK_RefundRequests_Classes_ClassID" FOREIGN KEY ("ClassID") REFERENCES "Classes" ("ClassID") ON DELETE CASCADE,
        CONSTRAINT "FK_RefundRequests_Users_RequesterUserId" FOREIGN KEY ("RequesterUserId") REFERENCES "Users" ("UserID") ON DELETE SET NULL,
        CONSTRAINT "FK_RefundRequests_Users_ReviewerID" FOREIGN KEY ("ReviewerID") REFERENCES "Users" ("UserID") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604173831_AddRefundsStudentAvailabilityReviewHidden') THEN
    CREATE INDEX "IX_RefundRequests_ClassID" ON "RefundRequests" ("ClassID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604173831_AddRefundsStudentAvailabilityReviewHidden') THEN
    CREATE INDEX "IX_RefundRequests_RequesterUserId" ON "RefundRequests" ("RequesterUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604173831_AddRefundsStudentAvailabilityReviewHidden') THEN
    CREATE INDEX "IX_RefundRequests_ReviewerID" ON "RefundRequests" ("ReviewerID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604173831_AddRefundsStudentAvailabilityReviewHidden') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260604173831_AddRefundsStudentAvailabilityReviewHidden', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604175110_AddAppointmentsAndExamAiConfig') THEN
    CREATE TABLE "Appointments" (
        "AppointmentID" uuid NOT NULL,
        "Title" text NOT NULL,
        "Description" text,
        "WithName" text,
        "WithUserId" uuid,
        "ScheduledAt" timestamp with time zone NOT NULL,
        "Status" integer NOT NULL,
        "Notes" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Appointments" PRIMARY KEY ("AppointmentID"),
        CONSTRAINT "FK_Appointments_Users_WithUserId" FOREIGN KEY ("WithUserId") REFERENCES "Users" ("UserID") ON DELETE SET NULL
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604175110_AddAppointmentsAndExamAiConfig') THEN
    CREATE TABLE "ExamAiConfigs" (
        "ConfigID" uuid NOT NULL,
        "ProctoringEnabled" boolean NOT NULL,
        "FaceDetection" boolean NOT NULL,
        "FullscreenRequired" boolean NOT NULL,
        "CopyPasteBlocked" boolean NOT NULL,
        "TabSwitchLimit" integer NOT NULL,
        "AutoGenerateEnabled" boolean NOT NULL,
        "DefaultDifficulty" text NOT NULL,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_ExamAiConfigs" PRIMARY KEY ("ConfigID")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604175110_AddAppointmentsAndExamAiConfig') THEN
    CREATE INDEX "IX_Appointments_WithUserId" ON "Appointments" ("WithUserId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260604175110_AddAppointmentsAndExamAiConfig') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260604175110_AddAppointmentsAndExamAiConfig', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605091707_ParentChildLinkRequest') THEN
    CREATE TABLE "ParentChildLinkRequests" (
        "Id" uuid NOT NULL,
        "ParentID" uuid NOT NULL,
        "StudentID" uuid NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "RespondedAt" timestamp with time zone,
        CONSTRAINT "PK_ParentChildLinkRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ParentChildLinkRequests_Parents_ParentID" FOREIGN KEY ("ParentID") REFERENCES "Parents" ("ParentID") ON DELETE CASCADE,
        CONSTRAINT "FK_ParentChildLinkRequests_Students_StudentID" FOREIGN KEY ("StudentID") REFERENCES "Students" ("StudentID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605091707_ParentChildLinkRequest') THEN
    CREATE INDEX "IX_ParentChildLinkRequests_ParentID" ON "ParentChildLinkRequests" ("ParentID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605091707_ParentChildLinkRequest') THEN
    CREATE INDEX "IX_ParentChildLinkRequests_StudentID" ON "ParentChildLinkRequests" ("StudentID");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605091707_ParentChildLinkRequest') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260605091707_ParentChildLinkRequest', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605155756_ClassRequest') THEN
    CREATE TABLE "ClassRequests" (
        "Id" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "SubjectId" uuid NOT NULL,
        "Grade" integer NOT NULL,
        "PreferredSchedule" text,
        "Budget" integer,
        "Note" text,
        "Status" text NOT NULL,
        "AssignedTutorId" uuid,
        "AcceptedSubmissionId" integer,
        "CreatedAt" timestamp with time zone NOT NULL,
        "AssignedAt" timestamp with time zone,
        CONSTRAINT "PK_ClassRequests" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_ClassRequests_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("StudentID") ON DELETE RESTRICT,
        CONSTRAINT "FK_ClassRequests_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("SubjectID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605155756_ClassRequest') THEN
    CREATE INDEX "IX_ClassRequests_StudentId" ON "ClassRequests" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605155756_ClassRequest') THEN
    CREATE INDEX "IX_ClassRequests_SubjectId" ON "ClassRequests" ("SubjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605155756_ClassRequest') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260605155756_ClassRequest', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605170821_TutorPost') THEN
    CREATE TABLE "TutorPosts" (
        "Id" uuid NOT NULL,
        "TutorId" uuid NOT NULL,
        "SubjectId" uuid NOT NULL,
        "GradeLevels" text,
        "HourlyRate" integer,
        "PreferredSchedule" text,
        "Note" text,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_TutorPosts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TutorPosts_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("SubjectID") ON DELETE RESTRICT,
        CONSTRAINT "FK_TutorPosts_Tutors_TutorId" FOREIGN KEY ("TutorId") REFERENCES "Tutors" ("TutorID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605170821_TutorPost') THEN
    CREATE INDEX "IX_TutorPosts_SubjectId" ON "TutorPosts" ("SubjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605170821_TutorPost') THEN
    CREATE INDEX "IX_TutorPosts_TutorId" ON "TutorPosts" ("TutorId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605170821_TutorPost') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260605170821_TutorPost', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605175411_AiTestAndApplications') THEN
    CREATE TABLE "AiTestAttempts" (
        "Id" uuid NOT NULL,
        "TutorId" uuid NOT NULL,
        "SubjectId" uuid NOT NULL,
        "QuestionsJson" text NOT NULL,
        "Score" integer,
        "Passed" boolean NOT NULL,
        "Used" boolean NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "SubmittedAt" timestamp with time zone,
        CONSTRAINT "PK_AiTestAttempts" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_AiTestAttempts_Subjects_SubjectId" FOREIGN KEY ("SubjectId") REFERENCES "Subjects" ("SubjectID") ON DELETE RESTRICT
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605175411_AiTestAndApplications') THEN
    CREATE TABLE "TutorPostApplications" (
        "Id" uuid NOT NULL,
        "TutorPostId" uuid NOT NULL,
        "StudentId" uuid NOT NULL,
        "TutorId" uuid NOT NULL,
        "SubjectId" uuid NOT NULL,
        "Status" text NOT NULL,
        "CreatedAt" timestamp with time zone NOT NULL,
        "RespondedAt" timestamp with time zone,
        CONSTRAINT "PK_TutorPostApplications" PRIMARY KEY ("Id"),
        CONSTRAINT "FK_TutorPostApplications_Students_StudentId" FOREIGN KEY ("StudentId") REFERENCES "Students" ("StudentID") ON DELETE RESTRICT,
        CONSTRAINT "FK_TutorPostApplications_TutorPosts_TutorPostId" FOREIGN KEY ("TutorPostId") REFERENCES "TutorPosts" ("Id") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605175411_AiTestAndApplications') THEN
    CREATE INDEX "IX_AiTestAttempts_SubjectId" ON "AiTestAttempts" ("SubjectId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605175411_AiTestAndApplications') THEN
    CREATE INDEX "IX_TutorPostApplications_StudentId" ON "TutorPostApplications" ("StudentId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605175411_AiTestAndApplications') THEN
    CREATE INDEX "IX_TutorPostApplications_TutorPostId" ON "TutorPostApplications" ("TutorPostId");
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260605175411_AiTestAndApplications') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260605175411_AiTestAndApplications', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260609164429_AddRooms') THEN
    CREATE TABLE "Rooms" (
        "RoomID" uuid NOT NULL,
        "Name" text NOT NULL,
        "Capacity" integer NOT NULL,
        "Type" text NOT NULL,
        "Building" text,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_Rooms" PRIMARY KEY ("RoomID")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260609164429_AddRooms') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260609164429_AddRooms', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260610132649_AddEmailOtp') THEN
    CREATE TABLE "EmailOtps" (
        "Id" uuid NOT NULL,
        "Email" text NOT NULL,
        "CodeHash" text NOT NULL,
        "Purpose" text NOT NULL,
        "ExpiresAt" timestamp with time zone NOT NULL,
        "Attempts" integer NOT NULL,
        "Consumed" boolean NOT NULL,
        "VerifiedAt" timestamp with time zone,
        "CreatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_EmailOtps" PRIMARY KEY ("Id")
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260610132649_AddEmailOtp') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260610132649_AddEmailOtp', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618165714_AddLearningStreak') THEN
    CREATE TABLE "LearningStreaks" (
        "UserID" uuid NOT NULL,
        "CurrentStreak" integer NOT NULL,
        "LongestStreak" integer NOT NULL,
        "LastActivityDate" date,
        "UpdatedAt" timestamp with time zone NOT NULL,
        CONSTRAINT "PK_LearningStreaks" PRIMARY KEY ("UserID"),
        CONSTRAINT "FK_LearningStreaks_Users_UserID" FOREIGN KEY ("UserID") REFERENCES "Users" ("UserID") ON DELETE CASCADE
    );
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260618165714_AddLearningStreak') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260618165714_AddLearningStreak', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702054615_AddClassRequestDurationMonths') THEN
    ALTER TABLE "ClassRequests" ADD "DurationMonths" integer;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702054615_AddClassRequestDurationMonths') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260702054615_AddClassRequestDurationMonths', '10.0.8');
    END IF;
END $EF$;
COMMIT;

START TRANSACTION;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702100158_AddWithdrawalLedgerTransactionId') THEN
    ALTER TABLE "Withdrawals" ADD "LedgerTransactionId" uuid;
    END IF;
END $EF$;

DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM "__EFMigrationsHistory" WHERE "MigrationId" = '20260702100158_AddWithdrawalLedgerTransactionId') THEN
    INSERT INTO "__EFMigrationsHistory" ("MigrationId", "ProductVersion")
    VALUES ('20260702100158_AddWithdrawalLedgerTransactionId', '10.0.8');
    END IF;
END $EF$;
COMMIT;

