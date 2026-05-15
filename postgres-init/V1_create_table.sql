-- ===========================================
-- SEED DATA
-- ===========================================

INSERT INTO "Users" ("Id", "LastName", "FirstName")
SELECT * FROM (
    VALUES 
        (gen_random_uuid(), 'Nguyễn', 'Huy'),
        (gen_random_uuid(), 'Quý', 'Khánh')
) AS v(Id, LastName, FirstName)
WHERE EXISTS (SELECT 1 FROM pg_class WHERE relname = 'Users');
