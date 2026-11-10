using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using NutriPlan.Core.Diet;
using NutriPlan.Core.Foods;
using NutriPlan.Core.Models;
using NutriPlan.Web;
using NutriPlan.Web.Models;
using NutriPlan.Web.Pages;
using NutriPlan.Web.Services;

namespace NutriPlan.Tests;

public class UiTextTests
{
    [Fact]
    public void Number_UsaFormatoBrasileiro()
    {
        Assert.Equal("1.234,5", UiText.Number(1234.5, 1));
        Assert.Equal("2.011", UiText.Number(2011));
    }

    [Fact]
    public void Date_FormataDiaMesAno()
    {
        var data = new DateTime(2026, 11, 9, 12, 0, 0, DateTimeKind.Local);

        Assert.Equal("09/11/2026", UiText.Date(data));
    }

    [Theory]
    [InlineData(1, "Segunda")]
    [InlineData(2, "Terça")]
    [InlineData(7, "Domingo")]
    [InlineData(8, "Segunda")]
    public void WeekDay_RetornaONomeDoDia(int dia, string esperado)
    {
        Assert.Equal(esperado, UiText.WeekDay(dia));
    }

    [Fact]
    public void Label_Sexo_RetornaTextoEmPortugues()
    {
        Assert.Equal("Masculino", UiText.Label(Sex.Male));
        Assert.Equal("Feminino", UiText.Label(Sex.Female));
    }

    [Fact]
    public void Label_Objetivo_RetornaTextoEmPortugues()
    {
        Assert.Equal("Emagrecer", UiText.Label(Goal.Lose));
        Assert.Equal("Ganhar peso", UiText.Label(Goal.Gain));
        Assert.Equal("Manter o peso", UiText.Label(Goal.Maintain));
    }

    [Theory]
    [InlineData(ActivityLevel.Sedentary, "Sedentário")]
    [InlineData(ActivityLevel.Light, "Levemente ativo")]
    [InlineData(ActivityLevel.Moderate, "Moderadamente ativo")]
    [InlineData(ActivityLevel.Heavy, "Muito ativo")]
    [InlineData(ActivityLevel.Extreme, "Extremamente ativo")]
    public void Label_NivelDeAtividade_ComecaComONomeDoNivel(ActivityLevel nivel, string esperado)
    {
        Assert.StartsWith(esperado, UiText.Label(nivel));
    }

    [Theory]
    [InlineData(MealType.Breakfast, "Café da manhã")]
    [InlineData(MealType.Lunch, "Almoço")]
    [InlineData(MealType.Snack, "Lanche da tarde")]
    [InlineData(MealType.Dinner, "Jantar")]
    public void Label_Refeicao_RetornaTextoEmPortugues(MealType tipo, string esperado)
    {
        Assert.Equal(esperado, UiText.Label(tipo));
    }

    [Theory]
    [InlineData(FoodRole.Carb, "Carboidrato")]
    [InlineData(FoodRole.Protein, "Proteína")]
    [InlineData(FoodRole.Dairy, "Laticínio")]
    [InlineData(FoodRole.Fruit, "Fruta")]
    [InlineData(FoodRole.Vegetable, "Vegetal")]
    public void Label_PapelDoAlimento_RetornaTextoEmPortugues(FoodRole papel, string esperado)
    {
        Assert.Equal(esperado, UiText.Label(papel));
    }
}

public class AboutAndErrorPageTests
{
    [Fact]
    public void About_ExpoeAEquipeRecebida()
    {
        var equipe = new TeamInfo
        {
            Course = "GQS",
            Professor = "Professor",
            Members = { new TeamMember { Name = "Aluno", Ra = "123" } }
        };

        var pagina = new AboutModel(equipe);

        Assert.Same(equipe, pagina.Team);
        Assert.Single(pagina.Team.Members);
    }

    [Fact]
    public void Error_SemRequisicao_NaoMostraIdDaRequisicao()
    {
        var pagina = new ErrorModel();

        Assert.False(pagina.ShowRequestId);
    }

    [Fact]
    public void Error_OnGet_PreencheOIdDaRequisicao()
    {
        var pagina = new ErrorModel
        {
            PageContext = new PageContext { HttpContext = new DefaultHttpContext { TraceIdentifier = "trace-123" } }
        };

        pagina.OnGet();

        Assert.False(string.IsNullOrEmpty(pagina.RequestId));
        Assert.True(pagina.ShowRequestId);
    }
}

public class FoodsPageTests : IClassFixture<DatabaseFixture>
{
    private sealed class FakeFoodApi : IFoodApi
    {
        private readonly FoodApiResult _result;

        public FakeFoodApi(FoodApiResult result) => _result = result;

        public string? LastTerm { get; private set; }

        public Task<FoodApiResult> SearchAsync(string term, CancellationToken cancellationToken = default)
        {
            LastTerm = term;
            return Task.FromResult(_result);
        }
    }

    private readonly DatabaseFixture _db;

    public FoodsPageTests(DatabaseFixture db) => _db = db;

    private static FoodApiResult Success() => new(new List<ExternalFood> { new("Biscoito", 450) }, null);

    [Fact]
    public async Task OnGet_SemBusca_ListaTodosOsAlimentosELocaisSemChamarAApi()
    {
        var api = new FakeFoodApi(Success());
        var pagina = new FoodsModel(_db.Foods, api);

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(_db.Foods.GetAll().Count, pagina.LocalFoods.Count);
        Assert.Null(pagina.External);
        Assert.Null(api.LastTerm);
    }

    [Fact]
    public async Task OnGet_BuscaSoComEspacos_SeComportaComoSemBusca()
    {
        var api = new FakeFoodApi(Success());
        var pagina = new FoodsModel(_db.Foods, api) { Q = "   " };

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.Equal(_db.Foods.GetAll().Count, pagina.LocalFoods.Count);
        Assert.Null(pagina.External);
    }

    [Fact]
    public async Task OnGet_ComBusca_FiltraLocaisEConsultaAApiComOTermoSemEspacos()
    {
        var api = new FakeFoodApi(Success());
        var pagina = new FoodsModel(_db.Foods, api) { Q = "  arroz " };

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.NotEmpty(pagina.LocalFoods);
        Assert.All(pagina.LocalFoods, f => Assert.Contains("arroz", f.Name, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("arroz", api.LastTerm);
        Assert.NotNull(pagina.External);
        Assert.True(pagina.External!.Succeeded);
    }

    [Fact]
    public async Task OnGet_ApiComErro_MantemOsAlimentosLocaisEMostraOErro()
    {
        var api = new FakeFoodApi(new FoodApiResult(new List<ExternalFood>(), "Sem conexão"));
        var pagina = new FoodsModel(_db.Foods, api) { Q = "arroz" };

        await pagina.OnGetAsync(CancellationToken.None);

        Assert.NotEmpty(pagina.LocalFoods);
        Assert.False(pagina.External!.Succeeded);
    }
}

public class IndexPageTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _db;

    public IndexPageTests(DatabaseFixture db) => _db = db;

    private static IndexModel.ProfileInput ValidInput() => new()
    {
        Name = "  Ana  ",
        Sex = Sex.Female,
        Age = 30,
        WeightKg = 70,
        HeightCm = 165,
        Activity = ActivityLevel.Light,
        Goal = Goal.Lose,
        WeeklyGoalKg = 0.5
    };

    [Fact]
    public void OnGet_SemExemplo_ComecaComFormularioVazio()
    {
        var pagina = new IndexModel(_db.Profiles);

        pagina.OnGet();

        Assert.Null(pagina.Input.Name);
        Assert.Null(pagina.Input.Age);
    }

    [Fact]
    public void OnGet_ComExemplo_PreencheOFormularioComOCasoDoTrabalho()
    {
        var pagina = new IndexModel(_db.Profiles);

        pagina.OnGet(example: true);

        Assert.Equal(19, pagina.Input.Age);
        Assert.Equal(112, pagina.Input.WeightKg);
        Assert.Equal(170, pagina.Input.HeightCm);
        Assert.Equal(Goal.Lose, pagina.Input.Goal);
    }

    [Fact]
    public void OnPost_CamposNumericosVazios_VoltaParaAPaginaComMensagem()
    {
        var pagina = new IndexModel(_db.Profiles);

        var resultado = pagina.OnPost();

        Assert.IsType<PageResult>(resultado);
        Assert.Single(pagina.Errors);
        Assert.Contains("Preencha", pagina.Errors[0]);
    }

    [Fact]
    public void OnPost_MenorDeIdade_VoltaParaAPaginaComErroDeValidacao()
    {
        var input = ValidInput();
        input.Age = 17;
        var pagina = new IndexModel(_db.Profiles) { Input = input };

        var resultado = pagina.OnPost();

        Assert.IsType<PageResult>(resultado);
        Assert.Contains(pagina.Errors, e => e.Contains("18 anos"));
    }

    [Fact]
    public void OnPost_PerfilValido_SalvaNoBancoERedirecionaParaOPlano()
    {
        var pagina = new IndexModel(_db.Profiles) { Input = ValidInput() };

        var resultado = pagina.OnPost();

        var redirecionamento = Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal("/Plan", redirecionamento.PageName);
        var id = Assert.IsType<int>(redirecionamento.RouteValues!["id"]);
        var salvo = _db.Profiles.Get(id);
        Assert.NotNull(salvo);
        Assert.Equal("Ana", salvo!.Name);
    }

    [Fact]
    public void ProfileInput_IsComplete_ExigeTodosOsCamposNumericos()
    {
        var input = ValidInput();
        Assert.True(input.IsComplete);

        input.HeightCm = null;
        Assert.False(input.IsComplete);
    }

    [Fact]
    public void ProfileInput_ToProfile_RemoveEspacosEUsaNomeVazioQuandoNulo()
    {
        var input = ValidInput();
        Assert.Equal("Ana", input.ToProfile().Name);

        input.Name = null;
        Assert.Equal("", input.ToProfile().Name);
    }
}

public class PlanPageTests : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _db;

    public PlanPageTests(DatabaseFixture db) => _db = db;

    private PlanModel CreatePage() => new(
        _db.Profiles,
        new DietGenerator(_db.Foods.GetAll()),
        new WeeklyProgressService(_db.Profiles));

    [Fact]
    public void OnGet_PerfilExistente_CalculaIndicadoresEMostraAPagina()
    {
        var id = _db.AddProfile();
        var pagina = CreatePage();

        var resultado = pagina.OnGet(id);

        Assert.IsType<PageResult>(resultado);
        Assert.Equal(id, pagina.Profile.Id);
        Assert.True(pagina.Imc > 0);
        Assert.False(string.IsNullOrEmpty(pagina.ImcClass));
        Assert.True(pagina.Target > 0);
        Assert.Equal(pagina.Weights.Count + 1, pagina.NextWeek);
    }

    [Fact]
    public void OnGet_PerfilInexistente_RetornaNaoEncontrado()
    {
        var resultado = CreatePage().OnGet(9999);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public void OnPostWeight_SemPeso_PedeOPesoDaSemana()
    {
        var id = _db.AddProfile();
        var pagina = CreatePage();

        var resultado = pagina.OnPostWeight(id, null);

        Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal("error", pagina.MessageKind);
        Assert.Contains("peso", pagina.Message!);
    }

    [Fact]
    public void OnPostWeight_MetaCumprida_MostraMensagemDeSucesso()
    {
        var id = _db.AddProfile(weight: 100, weeklyGoal: 1.0);
        var pagina = CreatePage();

        var resultado = pagina.OnPostWeight(id, 98.5);

        Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal("success", pagina.MessageKind);
        Assert.Contains("cumprida", pagina.Message!);
    }

    [Fact]
    public void OnPostWeight_MetaNaoCumprida_MostraMensagemInformativa()
    {
        var id = _db.AddProfile(weight: 100, weeklyGoal: 1.0);
        var pagina = CreatePage();

        var resultado = pagina.OnPostWeight(id, 99.8);

        Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal("info", pagina.MessageKind);
    }

    [Fact]
    public void OnPostWeight_PesoInvalido_MostraMensagemDeErro()
    {
        var id = _db.AddProfile();
        var pagina = CreatePage();

        var resultado = pagina.OnPostWeight(id, -10);

        Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal("error", pagina.MessageKind);
    }

    [Fact]
    public void OnPostWeight_PerfilInexistente_RetornaNaoEncontrado()
    {
        var resultado = CreatePage().OnPostWeight(9999, 80);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public void OnPostRegenerate_PerfilExistente_GeraNovoCardapio()
    {
        var id = _db.AddProfile();
        var pagina = CreatePage();

        var resultado = pagina.OnPostRegenerate(id);

        Assert.IsType<RedirectToPageResult>(resultado);
        Assert.Equal("success", pagina.MessageKind);
        Assert.Equal("Novo cardápio gerado.", pagina.Message);
    }

    [Fact]
    public void OnPostRegenerate_PerfilInexistente_RetornaNaoEncontrado()
    {
        var resultado = CreatePage().OnPostRegenerate(9999);

        Assert.IsType<NotFoundResult>(resultado);
    }
}
