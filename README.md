# Supermarket Simulator

Bem-vindo ao repositório do **Supermarket Simulator**! Este é um jogo multiplataforma desenvolvido em C# com o framework MonoGame.

## 📋 Pré-requisitos

Para rodar e modificar este projeto, você precisa ter instalado na sua máquina:

- [.NET SDK](https://dotnet.microsoft.com/download) (Recomendado a versão mais recente)
- **Para iOS:** Um Mac com o Xcode instalado.
- **Para Android:** Android SDK e emuladores configurados.

---

## 🚀 Primeira Vez na Máquina? (Setup Inicial)

**Sim, se você está em um computador novo (ou recém-formatado)**, você precisa instalar os *workloads* do .NET para que ele consiga compilar para Android e iOS. 

Abra o terminal e rode:

```bash
# Instala o suporte para compilar para Android
dotnet workload install android

# Instala o suporte para compilar para iOS (apenas no macOS)
dotnet workload install ios
```

> **Nota:** Esses comandos instalam as ferramentas globalmente no seu usuário (normalmente em `~/.dotnet/`). Você **não precisa rodá-los toda vez que clonar o projeto**, apenas uma vez por máquina.

---

## 📦 Clonando o Projeto

Se você já fez o passo acima, ou se a sua máquina já possui os workloads, basta clonar e preparar o projeto:

```bash
# Clone o repositório
git clone https://github.com/carlosxfelipe/supermarket-simulator.git

# Acesse a pasta do projeto
cd supermarket-simulator

# Restaure as dependências do NuGet (baixar pacotes)
dotnet restore
```

---

## 🎮 Como Rodar o Jogo

O Supermarket Simulator possui alvos diferentes para cada plataforma. Escolha abaixo a plataforma que deseja testar:

### Computador (Desktop)
A forma mais fácil e rápida de testar o jogo durante o desenvolvimento:
```bash
dotnet run --project SolarGame.Desktop
```

### Android
Certifique-se de que há um emulador Android rodando ou um dispositivo físico conectado em modo de depuração USB:
```bash
dotnet build SolarGame.Android -t:Run
```

### iOS
Certifique-se de ter um simulador ou dispositivo Apple devidamente configurado via Xcode:
```bash
dotnet build SolarGame.iOS -t:Run
```

---

## 💡 Dicas Adicionais

- O comando `dotnet restore` geralmente é acionado automaticamente pelos comandos de `run` ou `build`, mas é uma boa prática rodá-lo ao baixar código novo da internet.

---

## 📄 Licença

Este projeto é licenciado sob a [GNU General Public License v3.0](LICENSE).
