
using System.ComponentModel.DataAnnotations;

namespace FileManager.Dtos
{
    public class UserDto
    {
        //[Display(Name = "Имя")]
        //[Required(ErrorMessage = "Введите имя")]
        public string name { get; set; }

        //[Display(Name = "Фамилия")]
        //[Required(ErrorMessage = "Введите фамилию")]
        public string secondName { get; set; }

        //[Display(Name = "Логин")]
      //  [Required(ErrorMessage = "Введите логин")]
        public string login { get; set; }

      //  [Display(Name = "Пароль")]
      //  [Required(ErrorMessage = "Введите пароль")]
       // [StringLength(16, MinimumLength = 8, ErrorMessage = "Длина пароля должна быть от 8 до 16 символов")]
       // [RegularExpression(@"^(?=.*[a-zA-Z])(?=.*[0-9])$", ErrorMessage = "Пароль должен содержать только латинские символы и цифры")]
        public string password { get; set; }
        public int objectId { get; set; }
    }
}
