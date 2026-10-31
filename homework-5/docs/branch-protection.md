# Branch Protection Rules для ветки `main`

Репозиторий: `smart-campus-releases`, GitHub → Settings → Branches → Add branch ruleset
(или Rulesets → New ruleset, target: `main`).

| Правило | Значение | Зачем |
|---|---|---|
| Require a pull request before merging | ✅ | прямые push в `main` запрещены, любое изменение проходит ревью |
| Required approvals | **2** (один из них — CODEOWNERS для `homework-5/db/**`) | изменения схемы с финансовыми данными смотрит DBA |
| Dismiss stale approvals when new commits are pushed | ✅ | одобрение относится к конкретному коду |
| Require review from Code Owners | ✅ | `db/` → DBA, `src/` → команда столовой |
| Require status checks to pass | ✅ `build`, `test`, `sql-lint` | красная сборка не попадает в `main` |
| Require branches to be up to date before merging | ✅ | проверки идут на актуальном коде |
| Require conversation resolution | ✅ | нерешенные замечания блокируют merge |
| Require signed commits | ✅ | подтвержденный автор изменений |
| Require linear history | ✅ (squash merge) | один коммит `[CAMPUS-XXX] ...` на задачу |
| Do not allow bypassing the above settings | ✅ | правила действуют и для администраторов |
| Allow force pushes / deletions | ❌ | история `main` неизменна |

Соглашение о коммитах: `[CAMPUS-XXX] Краткое описание` — номер задачи связывает коммит,
CR, трассу (`jira.issue`) и запись CHANGELOG.

Пример конфигурации обязательных проверок (`.github/workflows/ci.yml`):

```yaml
name: ci
on: [pull_request]
jobs:
  build:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet build homework-5/CampusCanteen.sln -c Release
  test:
    needs: build
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - uses: actions/setup-dotnet@v4
        with: { dotnet-version: '8.0.x' }
      - run: dotnet test homework-5/CampusCanteen.sln -c Release
```
