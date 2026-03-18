using System.ComponentModel.DataAnnotations;

namespace Swoosh.Api.Dtos;

public class DeleteAccountDto
{
    [Required]
    public string Email { get; set; } = null!;

    [Required]
    public string Password { get; set; } = null!;
}
