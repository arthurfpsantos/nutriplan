using NutriPlan.Core.Models;
using NutriPlan.Core.Validation;

namespace NutriPlan.Tests;

public class ProfileValidatorTests
{
    private static UserProfile Valid() => new()
    {
        Name = "Ana",
        Sex = Sex.Female,
        Age = 25,
        WeightKg = 70,
        HeightCm = 165,
        Activity = ActivityLevel.Light,
        Goal = Goal.Lose,
        WeeklyGoalKg = 0.5
    };

    [Fact]
    public void Validate_PerfilValido_NaoRetornaErros()
    {
        Assert.Empty(ProfileValidator.Validate(Valid()));
    }

    [Theory]
    [InlineData(18)]
    [InlineData(100)]
    public void Validate_IdadeNosLimites_NaoRetornaErros(int idade)
    {
        var perfil = Valid();
        perfil.Age = idade;

        Assert.Empty(ProfileValidator.Validate(perfil));
    }

    [Fact]
    public void Validate_MenorDeIdade_RetornaErro()
    {
        var perfil = Valid();
        perfil.Age = 17;

        Assert.Contains(ProfileValidator.Validate(perfil), e => e.Contains("18 anos"));
    }

    [Fact]
    public void Validate_IdadeAcimaDoLimite_RetornaErro()
    {
        var perfil = Valid();
        perfil.Age = 101;

        Assert.Contains(ProfileValidator.Validate(perfil), e => e.Contains("Idade"));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(400)]
    public void Validate_PesoNosLimites_NaoRetornaErros(double peso)
    {
        var perfil = Valid();
        perfil.WeightKg = peso;

        Assert.Empty(ProfileValidator.Validate(perfil));
    }

    [Theory]
    [InlineData(-5)]
    [InlineData(0)]
    [InlineData(10)]
    [InlineData(29.9)]
    [InlineData(400.1)]
    [InlineData(500)]
    public void Validate_PesoInvalido_RetornaErro(double peso)
    {
        var perfil = Valid();
        perfil.WeightKg = peso;

        Assert.Contains(ProfileValidator.Validate(perfil), e => e.Contains("Peso"));
    }

    [Theory]
    [InlineData(100)]
    [InlineData(250)]
    public void Validate_AlturaNosLimites_NaoRetornaErros(double altura)
    {
        var perfil = Valid();
        perfil.HeightCm = altura;

        Assert.Empty(ProfileValidator.Validate(perfil));
    }

    [Theory]
    [InlineData(-170)]
    [InlineData(50)]
    [InlineData(99.9)]
    [InlineData(250.1)]
    [InlineData(300)]
    public void Validate_AlturaInvalida_RetornaErro(double altura)
    {
        var perfil = Valid();
        perfil.HeightCm = altura;

        Assert.Contains(ProfileValidator.Validate(perfil), e => e.Contains("Altura"));
    }

    [Fact]
    public void Validate_NomeVazio_RetornaErro()
    {
        var perfil = Valid();
        perfil.Name = "  ";

        Assert.Contains(ProfileValidator.Validate(perfil), e => e.Contains("Nome"));
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(1.0)]
    public void Validate_MetaSemanalNosLimites_NaoRetornaErros(double meta)
    {
        var perfil = Valid();
        perfil.WeeklyGoalKg = meta;

        Assert.Empty(ProfileValidator.Validate(perfil));
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(0.09)]
    [InlineData(1.01)]
    [InlineData(1.5)]
    public void Validate_MetaSemanalForaDoLimite_RetornaErro(double meta)
    {
        var perfil = Valid();
        perfil.WeeklyGoalKg = meta;

        Assert.Contains(ProfileValidator.Validate(perfil), e => e.Contains("Meta semanal"));
    }

    [Fact]
    public void Validate_VariosProblemas_RetornaTodosOsErros()
    {
        var perfil = Valid();
        perfil.Name = "";
        perfil.Age = 10;
        perfil.WeightKg = -1;

        Assert.Equal(3, ProfileValidator.Validate(perfil).Count);
    }

    [Fact]
    public void ValidateWeight_PesoSemanalInvalido_RetornaErro()
    {
        Assert.NotNull(ProfileValidator.ValidateWeight(-3));
        Assert.Null(ProfileValidator.ValidateWeight(80.5));
    }
}
