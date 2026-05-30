namespace UNI_EDU_Backend.Application.DTOs.Tutors;

// Body for POST /api/tutors/me/bank-account. All three fields required (validator).
// POST acts as upsert: saving when nothing exists, overwriting when one already does.
public class SaveBankAccountRequest
{
    public string BankName { get; set; } = string.Empty;
    public string BankAccount { get; set; } = string.Empty;
    public string BankAccountHolder { get; set; } = string.Empty;
}
