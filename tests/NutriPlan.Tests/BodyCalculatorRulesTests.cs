using NutriPlan.Core.Calculations;
using NutriPlan.Core.Models;

namespace NutriPlan.Tests;

public class BodyCalculatorRulesTests
{
    private static UserProfile ExampleProfile(Goal goal, int extraKcal = 0) => new()
    {
        Name = "Exemplo",
        Sex = Sex.Male,
        Age = 19,
        WeightKg = 112,
        HeightCm = 170,
        Activity = ActivityLevel.Sedentary,
        Goal = goal,
        WeeklyGoalKg = 0.5,
        ExtraAdjustmentKcal = extraKcal
    };

    [Fact]
    public void ActivityFactor_NivelInexistente_LancaExcecao()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => BodyCalculator.ActivityFactor((ActivityLevel)99));
    }

    [Fact]
    public void Tmb_MesmosDados_HomemTem166KcalAMaisQueMulher()
    {
        var homem = BodyCalculator.Tmb(Sex.Male, 70, 175, 30);
        var mulher = BodyCalculator.Tmb(Sex.Female, 70, 175, 30);

        Assert.Equal(166, homem - mulher, 6);
    }

    [Theory]
    [InlineData(ActivityLevel.Sedentary, 2400)]
    [InlineData(ActivityLevel.Extreme, 3800)]
    public void DailyEnergy_MultiplicaTmbPeloFator(ActivityLevel nivel, double esperado)
    {
        Assert.Equal(esperado, BodyCalculator.DailyEnergy(2000, nivel), 6);
    }

    [Fact]
    public void Imc_MesmoPesoComMaisAltura_DiminuiOImc()
    {
        Assert.True(BodyCalculator.Imc(80, 180) < BodyCalculator.Imc(80, 160));
    }

    [Fact]
    public void CalorieTarget_Ganhar_ComAjusteExtra_SomaAoSuperavit()
    {
        Assert.Equal(3111, BodyCalculator.CalorieTarget(ExampleProfile(Goal.Gain, 100)), 0);
    }

    [Fact]
    public void CalorieTarget_Manter_IgnoraAjusteExtra()
    {
        Assert.Equal(2511, BodyCalculator.CalorieTarget(ExampleProfile(Goal.Maintain, 200)), 0);
    }

    [Fact]
    public void CalorieTarget_ArredondaParaNumeroInteiro()
    {
        var perfil = new UserProfile
        {
            Sex = Sex.Male,
            Age = 30,
            WeightKg = 70,
            HeightCm = 175,
            Activity = ActivityLevel.Moderate,
            Goal = Goal.Maintain,
            WeeklyGoalKg = 0.5
        };

        Assert.Equal(2556, BodyCalculator.CalorieTarget(perfil), 0);
    }

    [Fact]
    public void CalorieTarget_Homem_NuncaFicaAbaixoDoPisoDeSeguranca()
    {
        var perfil = new UserProfile
        {
            Sex = Sex.Male,
            Age = 80,
            WeightKg = 45,
            HeightCm = 150,
            Activity = ActivityLevel.Sedentary,
            Goal = Goal.Lose,
            WeeklyGoalKg = 1
        };

        Assert.Equal(BodyCalculator.MinKcalMale, BodyCalculator.CalorieTarget(perfil));
    }

    [Theory]
    [InlineData(Sex.Male, 1500)]
    [InlineData(Sex.Female, 1200)]
    public void MinimumKcal_RetornaPisoDeCadaSexo(Sex sexo, double esperado)
    {
        Assert.Equal(esperado, BodyCalculator.MinimumKcal(sexo));
    }
}
