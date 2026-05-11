# TimeControl Linhalis

TimeControl Linhalis é um sistema de controle de tempo baseado em eventos de atividade do desktop.

A aplicação web permite cadastrar projetos e regras de identificação de trabalho. O agente desktop para Windows coleta eventos da janela ativa em segundo plano e envia esses eventos para a API. A API processa os eventos e monta sessões de trabalho para relatórios, dashboards e análise por projeto.

## Componentes

- **Aplicação web + API**: roda em Docker como uma única imagem, com frontend React e API ASP.NET publicados juntos.
- **PostgreSQL**: banco de dados usado pela API.
- **Agente desktop Windows**: aplicativo WinForms que fica na bandeja do sistema e envia eventos para a API usando um token do perfil.

## Imagem Docker

A imagem pública da aplicação web/API está no Docker Hub:

```text
rovann/timecontrol-linhalis:latest
```

Essa imagem já contém:

- API ASP.NET;
- frontend React compilado;
- arquivos estáticos servidos pela própria API;
- aplicação ouvindo na porta `8080` dentro do container.

## Instalação Com Docker

Crie uma pasta para a instalação e adicione um arquivo `docker-compose.yml`:

```yaml
name: timecontrol-linhalis

services:
  db:
    image: postgres:18-alpine
    container_name: timecontrol-linhalis-db
    restart: unless-stopped
    environment:
      POSTGRES_DB: ${POSTGRES_DB:-timecontrol_linhalis}
      POSTGRES_USER: ${POSTGRES_USER:-timecontrol_linhalis}
      POSTGRES_PASSWORD: ${POSTGRES_PASSWORD:-change-this-password}
    volumes:
      - timecontrol-linhalis-db:/var/lib/postgresql/data
    healthcheck:
      test: ["CMD-SHELL", "pg_isready -U ${POSTGRES_USER:-timecontrol_linhalis} -d ${POSTGRES_DB:-timecontrol_linhalis}"]
      interval: 10s
      timeout: 5s
      retries: 10

  app:
    image: ${TIMECONTROL_LINHALIS_IMAGE:-rovann/timecontrol-linhalis:latest}
    container_name: timecontrol-linhalis-app
    restart: unless-stopped
    depends_on:
      db:
        condition: service_healthy
    ports:
      - "${TIMECONTROL_LINHALIS_PORT:-8080}:8080"
    environment:
      ASPNETCORE_ENVIRONMENT: Production
      ASPNETCORE_URLS: http://+:8080
      ConnectionStrings__DefaultConnection: "Server=db;Port=5432;Database=${POSTGRES_DB:-timecontrol_linhalis};User Id=${POSTGRES_USER:-timecontrol_linhalis};Password=${POSTGRES_PASSWORD:-change-this-password};Include Error Detail=false"
      Jwt__Key: ${JWT_KEY:-replace-this-with-a-random-secret-of-at-least-32-chars}
      Jwt__Issuer: "${JWT_ISSUER:-TimeControl Linhalis}"
      Jwt__Audience: "${JWT_AUDIENCE:-TimeControl Linhalis}"
      Jwt__ExpirationHours: ${JWT_EXPIRATION_HOURS:-24}
      Cors__AllowedOrigins__0: ${PUBLIC_ORIGIN:-http://localhost:8080}

volumes:
  timecontrol-linhalis-db:
```

Crie também um arquivo `.env`:

```env
TIMECONTROL_LINHALIS_IMAGE=rovann/timecontrol-linhalis:latest
TIMECONTROL_LINHALIS_PORT=8080

POSTGRES_DB=timecontrol_linhalis
POSTGRES_USER=timecontrol_linhalis
POSTGRES_PASSWORD=troque-esta-senha

JWT_KEY=troque-por-uma-chave-aleatoria-com-pelo-menos-32-caracteres
JWT_ISSUER="TimeControl Linhalis"
JWT_AUDIENCE="TimeControl Linhalis"
JWT_EXPIRATION_HOURS=24

PUBLIC_ORIGIN=http://localhost:8080
```

Suba a aplicação:

```powershell
docker compose up -d
```

Acesse:

```text
http://localhost:8080
```

Em produção, ajuste `PUBLIC_ORIGIN` para o domínio público da aplicação, por exemplo:

```env
PUBLIC_ORIGIN=https://timecontrol.seudominio.com
```

## Atualização Da Aplicação

Para baixar a versão mais recente da imagem e reiniciar:

```powershell
docker compose pull app
docker compose up -d
```

Para parar:

```powershell
docker compose down
```

Para parar e apagar também o volume do banco:

```powershell
docker compose down -v
```

## Agente Desktop Windows

O agente desktop é o coletor público do TimeControl Linhalis. Ele roda no Windows, fica na bandeja do sistema e envia eventos para a API configurada.

Ele coleta, a cada amostragem:

- data e hora do evento;
- nome do processo ativo;
- título da janela ativa;
- nome do computador;
- usuário do Windows;
- posição X/Y do ponteiro do mouse.

O agente não captura tela, não grava teclado e não envia conteúdo de arquivos. Ele usa os eventos para permitir que a API identifique sessões de trabalho conforme as regras configuradas nos projetos.

## Download Do Agente

Quando houver uma versão publicada, baixe o executável em:

```text
[https://github.com/rovannlinhalis/timecontrol-linhalis/releases/latest](https://github.com/rovannlinhalis/timecontrol-linhalis/tree/main/agent/releases)
```

Baixe o arquivo do agente para uma pasta local no Windows e execute:

```text
TimeControl.Agent.exe
```

Na primeira execução, a janela de configuração será aberta automaticamente.

## Build Do Agente

Requisitos:

- Windows;
- .NET SDK com suporte a projeto .NET Framework;
- .NET Framework 4.5 ou superior instalado.

Clone o repositório do agente:

```powershell
git clone https://github.com/rovannlinhalis/timecontrol-linhalis.git
cd timecontrol-linhalis
```

Compile em Release:

```powershell
dotnet build TimeControl.Agent.WinForms.csproj -c Release
```

O executável será gerado em:

```text
bin\Release\TimeControl.Agent.exe
```

## Configuração Do Agente

No TimeControl Linhalis web:

1. Entre na sua conta.
2. Abra a tela de perfil.
3. Copie o token do agente desktop.
4. Copie o domínio da API.

No agente Windows:

1. Abra `TimeControl.Agent.exe`.
2. Informe o domínio da API, por exemplo:

```text
https://timecontrol.seudominio.com
```

3. Informe o token copiado no perfil.
4. Clique em `Testar`.
5. Clique em `Salvar`.

O endpoint de ingestão é fixo no agente. Informe apenas o domínio/base da API; não adicione `/api/ingest`.

## Uso Diário

Depois de configurado, o agente fica na bandeja do sistema.

Pelo menu do ícone é possível:

- abrir as configurações;
- sincronizar agora;
- sair do agente.

Se a API estiver offline ou o computador estiver sem conexão, os eventos ficam em cache local e são enviados depois.

## Arquivos Locais Do Agente

Configuração:

```text
%APPDATA%\TimeControl\settings.json
```

Cache local de eventos, ao lado do executável:

```text
yyyyMMdd.atc
```

## Segurança E Segredos

- Troque sempre `POSTGRES_PASSWORD`.
- Troque sempre `JWT_KEY`.
- Use uma `JWT_KEY` longa e aleatória.
- Não publique arquivos `.env` com senhas reais.
- Em produção, use HTTPS no domínio público.

## Repositórios E Imagens

- Código do agente desktop: `https://github.com/rovannlinhalis/timecontrol-linhalis`
- Imagem Docker da aplicação web/API: `rovann/timecontrol-linhalis:latest`
