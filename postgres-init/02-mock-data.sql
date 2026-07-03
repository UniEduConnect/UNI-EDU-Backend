-- ============================================================================
-- 02-mock-data.sql  —  Dữ liệu mẫu cho UNI-EDU-Backend
-- ----------------------------------------------------------------------------
-- File này chạy SAU 01-schema.sql (Postgres chạy các script trong
-- /docker-entrypoint-initdb.d theo thứ tự alphabet) nên các bảng đã tồn tại.
--
-- Mật khẩu của TẤT CẢ tài khoản: Password123!
-- (hash BCrypt bên dưới sinh bằng đúng thư viện BCrypt.Net-Next 4.2.0 mà
--  AuthService dùng, workFactor = 11).
--
-- Enum (lưu dưới dạng int):
--   UserRole : Admin=0, Tutor=1, Parent=2, Student=3, Office=4, Finance=5, ExamManager=6
--   UserStatus: Pending=0, Approved=1, Rejected=2, Suspended=3
--   TutorType : Tutor=0, Teacher=1
--
-- Mọi INSERT đều idempotent (ON CONFLICT DO NOTHING) để an toàn khi chạy lại.
-- ============================================================================

BEGIN;

-- ---------------------------------------------------------------------------
-- Subjects (môn học)
-- ---------------------------------------------------------------------------
INSERT INTO "Subjects" ("SubjectID", "SubjectName") VALUES
    ('10000000-0000-0000-0000-000000000001', 'Toán'),
    ('10000000-0000-0000-0000-000000000002', 'Vật lý'),
    ('10000000-0000-0000-0000-000000000003', 'Hóa học'),
    ('10000000-0000-0000-0000-000000000004', 'Tiếng Anh'),
    ('10000000-0000-0000-0000-000000000005', 'Ngữ văn'),
    ('10000000-0000-0000-0000-000000000006', 'Sinh học')
ON CONFLICT ("SubjectID") DO NOTHING;

-- ---------------------------------------------------------------------------
-- Users (1 Admin, 4 Tutor, 3 Student, 2 Parent) — pass: Password123!
-- ---------------------------------------------------------------------------
INSERT INTO "Users" ("UserID", "Fullname", "HashedPassword", "Email", "PhoneNumber", "Role", "Status", "CreatedAt") VALUES
    -- Admin
    ('a0000000-0000-0000-0000-000000000001', 'Quản trị viên',  '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'admin@uniedu.test',   '0900000001', 0, 1, now()),
    -- Tutors
    ('20000000-0000-0000-0000-000000000001', 'Nguyễn Văn An',   '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'tutor1@uniedu.test',  '0901000001', 1, 1, now()),
    ('20000000-0000-0000-0000-000000000002', 'Trần Thị Bình',   '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'tutor2@uniedu.test',  '0901000002', 1, 1, now()),
    ('20000000-0000-0000-0000-000000000003', 'Lê Hoàng Cường',  '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'tutor3@uniedu.test',  '0901000003', 1, 1, now()),
    ('20000000-0000-0000-0000-000000000004', 'Phạm Thùy Dung',  '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'tutor4@uniedu.test',  '0901000004', 1, 1, now()),
    -- Students
    ('30000000-0000-0000-0000-000000000001', 'Đỗ Minh Khang',   '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'student1@uniedu.test', '0902000001', 3, 1, now()),
    ('30000000-0000-0000-0000-000000000002', 'Vũ Gia Hân',      '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'student2@uniedu.test', '0902000002', 3, 1, now()),
    ('30000000-0000-0000-0000-000000000003', 'Bùi Tuấn Kiệt',   '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'student3@uniedu.test', '0902000003', 3, 1, now()),
    -- Parents
    ('40000000-0000-0000-0000-000000000001', 'Hoàng Văn Phụ',   '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'parent1@uniedu.test',  '0903000001', 2, 1, now()),
    ('40000000-0000-0000-0000-000000000002', 'Đặng Thị Mẫu',    '$2a$11$cmK8o/mamOwknfTt913GAun4q6SSLHHIeqQDHPSPI8xmdvCRi6nl.', 'parent2@uniedu.test',  '0903000002', 2, 1, now())
ON CONFLICT ("UserID") DO NOTHING;

-- ---------------------------------------------------------------------------
-- Tutors (1-1 với User; TutorID == UserID)
-- ---------------------------------------------------------------------------
INSERT INTO "Tutors" (
    "TutorID", "FullName", "Gender", "Degree", "Bio", "DateOfBirth",
    "AvatarUrl", "Location", "School", "HourlyRate", "YearsExperience",
    "IsVerified", "TeachingStyle", "TutorType", "AverageRating",
    "Certificates", "Achievements", "AvailableSlots"
) VALUES
    ('20000000-0000-0000-0000-000000000001', 'Nguyễn Văn An', 'Nam', 'Cử nhân Sư phạm Toán',
        'Giáo viên Toán THPT 8 năm kinh nghiệm, chuyên luyện thi đại học.', '1990-03-12T00:00:00Z',
        NULL, 'Hà Nội', 'ĐH Sư phạm Hà Nội', 250000, 8,
        true, 'Hệ thống hóa kiến thức, bám sát đề thi', 1, 4.8,
        ARRAY['Chứng chỉ nghiệp vụ sư phạm']::text[], ARRAY['Giải nhì GV giỏi cấp thành phố']::text[],
        '[{"day":"Mon","time":"18:00-20:00"},{"day":"Wed","time":"18:00-20:00"}]'::jsonb),

    ('20000000-0000-0000-0000-000000000002', 'Trần Thị Bình', 'Nữ', 'Cử nhân Ngôn ngữ Anh',
        'Gia sư Tiếng Anh, IELTS 8.0, chuyên giao tiếp và luyện thi.', '1997-07-25T00:00:00Z',
        NULL, 'TP. Hồ Chí Minh', 'ĐH Ngoại ngữ', 200000, 4,
        false, 'Giao tiếp tự nhiên, học qua tình huống', 0, 4.5,
        ARRAY['IELTS 8.0','TOEIC 990']::text[], ARRAY[]::text[],
        '[{"day":"Tue","time":"19:00-21:00"},{"day":"Thu","time":"19:00-21:00"},{"day":"Sat","time":"09:00-11:00"}]'::jsonb),

    ('20000000-0000-0000-0000-000000000003', 'Lê Hoàng Cường', 'Nam', 'Thạc sĩ Hóa học',
        'Gia sư Hóa - Sinh, kiên nhẫn, phù hợp học sinh mất gốc.', '1995-11-02T00:00:00Z',
        NULL, 'Đà Nẵng', 'ĐH Bách Khoa', 120000, 3,
        true, 'Đi từ cơ bản đến nâng cao', 0, 4.9,
        ARRAY[]::text[], ARRAY['Thủ khoa đầu vào ngành Hóa']::text[],
        '[{"day":"Mon","time":"20:00-22:00"},{"day":"Fri","time":"20:00-22:00"}]'::jsonb),

    ('20000000-0000-0000-0000-000000000004', 'Phạm Thùy Dung', 'Nữ', 'Cử nhân Sư phạm Ngữ văn',
        'Giáo viên Ngữ văn, luyện viết nghị luận và cảm thụ văn học.', '1992-05-18T00:00:00Z',
        NULL, 'Hà Nội', 'ĐH Sư phạm Hà Nội', 180000, 6,
        true, 'Khơi gợi tư duy, luyện đề chuyên sâu', 1, 4.2,
        ARRAY['Chứng chỉ nghiệp vụ sư phạm']::text[], ARRAY[]::text[],
        '[{"day":"Wed","time":"19:30-21:30"},{"day":"Sun","time":"14:00-16:00"}]'::jsonb)
ON CONFLICT ("TutorID") DO NOTHING;

-- ---------------------------------------------------------------------------
-- Parents (1-1 với User)
-- ---------------------------------------------------------------------------
INSERT INTO "Parents" ("ParentID", "FullName") VALUES
    ('40000000-0000-0000-0000-000000000001', 'Hoàng Văn Phụ'),
    ('40000000-0000-0000-0000-000000000002', 'Đặng Thị Mẫu')
ON CONFLICT ("ParentID") DO NOTHING;

-- ---------------------------------------------------------------------------
-- Students (1-1 với User; ParentID liên kết tới Parents — có thể NULL)
-- ---------------------------------------------------------------------------
INSERT INTO "Students" ("StudentID", "ParentID", "FullName", "School", "Grade", "AvailableSlots") VALUES
    ('30000000-0000-0000-0000-000000000001', '40000000-0000-0000-0000-000000000001', 'Đỗ Minh Khang', 'THPT Chu Văn An', 11,
        '[{"day":"Mon","time":"18:00-20:00"},{"day":"Wed","time":"18:00-20:00"}]'::jsonb),
    ('30000000-0000-0000-0000-000000000002', '40000000-0000-0000-0000-000000000002', 'Vũ Gia Hân', 'THPT Lê Quý Đôn', 12,
        '[{"day":"Tue","time":"19:00-21:00"}]'::jsonb),
    ('30000000-0000-0000-0000-000000000003', NULL, 'Bùi Tuấn Kiệt', 'THCS Nguyễn Du', 9,
        NULL)
ON CONFLICT ("StudentID") DO NOTHING;

-- ---------------------------------------------------------------------------
-- TutorSubjects (M2M Tutor <-> Subject)
-- ---------------------------------------------------------------------------
INSERT INTO "TutorSubjects" ("TutorID", "SubjectID") VALUES
    -- An: Toán, Vật lý
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000001'),
    ('20000000-0000-0000-0000-000000000001', '10000000-0000-0000-0000-000000000002'),
    -- Bình: Tiếng Anh
    ('20000000-0000-0000-0000-000000000002', '10000000-0000-0000-0000-000000000004'),
    -- Cường: Hóa học, Sinh học
    ('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000003'),
    ('20000000-0000-0000-0000-000000000003', '10000000-0000-0000-0000-000000000006'),
    -- Dung: Ngữ văn, Tiếng Anh
    ('20000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000005'),
    ('20000000-0000-0000-0000-000000000004', '10000000-0000-0000-0000-000000000004')
ON CONFLICT ("TutorID", "SubjectID") DO NOTHING;

-- ---------------------------------------------------------------------------
-- Wallets (1-1 với User) — số dư mẫu (VND)
-- ---------------------------------------------------------------------------
INSERT INTO "Wallets" ("UserID", "Balance", "EscrowBalance", "UpdatedAt") VALUES
    ('a0000000-0000-0000-0000-000000000001',        0, 0, now()),  -- Admin
    ('20000000-0000-0000-0000-000000000001',        0, 0, now()),  -- Tutor An
    ('20000000-0000-0000-0000-000000000002',        0, 0, now()),  -- Tutor Bình
    ('20000000-0000-0000-0000-000000000003',        0, 0, now()),  -- Tutor Cường
    ('20000000-0000-0000-0000-000000000004',        0, 0, now()),  -- Tutor Dung
    ('30000000-0000-0000-0000-000000000001',  2000000, 0, now()),  -- Student Khang
    ('30000000-0000-0000-0000-000000000002',  1500000, 0, now()),  -- Student Hân
    ('30000000-0000-0000-0000-000000000003',   500000, 0, now()),  -- Student Kiệt
    ('40000000-0000-0000-0000-000000000001',  5000000, 0, now()),  -- Parent Phụ
    ('40000000-0000-0000-0000-000000000002',  3000000, 0, now())   -- Parent Mẫu
ON CONFLICT ("UserID") DO NOTHING;

COMMIT;
