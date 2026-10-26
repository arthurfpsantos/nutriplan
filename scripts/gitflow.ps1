# Cria o histórico GitFlow do projeto (main, develop e feature/*) com commits semânticos.
# Requisito: Git instalado. Uso, na raiz do projeto:  powershell -File scripts/gitflow.ps1
$ErrorActionPreference = "Stop"

if (-not (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git não encontrado. Instale em https://git-scm.com e abra um novo terminal."
}

function Commit-Feature($branch, $message, [string[]]$paths) {
    git checkout develop | Out-Null
    git checkout -b $branch | Out-Null
    git add -- $paths
    git commit -m $message | Out-Null
    git checkout develop | Out-Null
    git merge --no-ff $branch -m "merge: $branch em develop" | Out-Null
    Write-Host "OK  $branch  ->  $message"
}

if (-not (Test-Path .git)) { git init -b main | Out-Null }

git add .gitignore .editorconfig
git commit -m "chore: inicializa repositorio com gitignore e editorconfig" | Out-Null
git checkout -b develop | Out-Null

Commit-Feature "feature/calculos-corporais" "feat(core): calcula IMC, TMB, gasto diario e meta calorica" `
    @("NutriPlan.slnx","src/NutriPlan.Core/Models", "src/NutriPlan.Core/Calculations/BodyCalculator.cs",
      "src/NutriPlan.Core/NutriPlan.Core.csproj", "tests/NutriPlan.Tests/NutriPlan.Tests.csproj",
      "tests/NutriPlan.Tests/BodyCalculatorTests.cs")

Commit-Feature "feature/validacao" "feat(core): valida perfil e peso com regras de seguranca" `
    @("src/NutriPlan.Core/Validation", "tests/NutriPlan.Tests/ProfileValidatorTests.cs")

Commit-Feature "feature/gerador-dieta" "feat(core): gera cardapio semanal com analise combinatoria" `
    @("src/NutriPlan.Core/Calculations/Combinatorics.cs", "src/NutriPlan.Core/Diet",
      "tests/NutriPlan.Tests/CombinatoricsTests.cs", "tests/NutriPlan.Tests/DietGeneratorTests.cs")

Commit-Feature "feature/conquistas" "feat(core): avalia metas semanais e concede conquistas" `
    @("src/NutriPlan.Core/Achievements", "tests/NutriPlan.Tests/ProgressEvaluatorTests.cs")

Commit-Feature "feature/persistencia-api" "feat(web): persiste dados em SQLite e consulta Open Food Facts" `
    @("src/NutriPlan.Core/Foods", "src/NutriPlan.Web/Data", "src/NutriPlan.Web/Services",
      "tests/NutriPlan.Tests/WebLayerTests.cs")

Commit-Feature "feature/site" "feat(web): cria paginas do site NutriPlan" `
    @("src/NutriPlan.Web")

Commit-Feature "feature/ci-docs" "ci: adiciona pipeline do GitHub Actions e README" `
    @(".github", "README.md", "scripts")

git checkout main | Out-Null
git merge --no-ff develop -m "release: versao inicial do NutriPlan" | Out-Null
git checkout develop | Out-Null
git branch --list
git log --oneline --graph --all
