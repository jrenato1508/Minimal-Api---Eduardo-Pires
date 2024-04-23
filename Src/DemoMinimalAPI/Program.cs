using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authorization;
using DemoMinimalAPI.Data;
using DemoMinimalAPI.Models;
using MiniValidation;
using NetDevPack.Identity;
using NetDevPack.Identity.Jwt;
using NetDevPack.Identity.Model;
using Microsoft.OpenApi.Models;

#region Configure Services
var builder = WebApplication.CreateBuilder(args);

#region Configuração Identity com o JWT token
#region OBS
/*
    Pessoal, quem estiver estudando (hoje) esse material tem que adicionar a biblioteca NetDevPack.Identity na versão 6.0.5 por que na última versão
    acaba gerado erro.

    Adicionamos dois serviços o AddIdentityEntityFrameworkContextConfiguration e tanbém o AddIdentityConfiguration
    
    e adicionando o app.UseAuthConfiguration(); no pipeline em cima do UseHttpsRedirection
 */
#endregion
builder.Services.AddIdentityEntityFrameworkContextConfiguration(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    b => b.MigrationsAssembly("DemoMinimalAPI")));
#endregion


builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityConfiguration();
builder.Services.AddJwtConfiguration(builder.Configuration, "AppSettings");

#region AddAuthorization - Policy/Claim
/*
     Configuração para usarmos Claim através das Policys. Estamos adicionando uma Policy com o nome ExcluirFornecedor que por usa vez requer uma Claim com
     o nome de ExcluirFornecedor.
     Agora para configurar a action/ Endpoint para exigir que além autenticação o usuário precisa ter determinada claim, adicionamos essa exigencia no metadado
     com a seguinte	informação .RequireAuthorization("ExcluirFornecedor") Vamos adicionar essa informação na action Map.Delete
	ex:
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization("ExcluirFornecedor")
			.WithName("DeleteFornecedor")
			.WithTags("Fornecedor");

     Agora vamos atribuir essa claim ao usuário que criamos, para isso basta pegar o Guid do usuário na tabela AspNetUsers e adicionar uma claim na
     tabela AspNetUserClaims com o claimType e ClaimValue como ExcluirFornecedor. O claim value não precisa mas vamos preencher de qualquer forma. Feito isso,
     podemos testar se o token que é gerado ao realizar o login contem as claim que adicionamos ao usuário, parra isso basta pegar o token e jogar no site
    jwt pra analisarmos.
 */
#endregion
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirFornecedor",
        policy => policy.RequireClaim("ExcluirFornecedor"));
});


builder.Services.AddEndpointsApiExplorer();

#region Configuração do Swagger
//builder.Services.AddSwaggerGen(); sai disso aqui para o trecho de código a seguir
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API Sample",
        Description = "Desenvolvido por José Renato - Orientado Por Eduardo Piress ",
        Contact = new OpenApiContact { Name = "José Renato", Email = "jrenato1508@gmail.com.net.br" },
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer + espaço + seu token",
        Name = "Authorization",
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] {}
        }
    });
});
#endregion
#endregion

#region Configure Pipeline

#region OBS Pipeline
/*
  Do var app = builder.Build(); pra baixo é a configuração do fluxo do request
  var app = builder.Build(); // construção da nossa api com todos os servições que foram adicionados na pipeline
 */
#endregion
var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthConfiguration();

app.UseHttpsRedirection();


AdicionandoActionOrEndpoint(app);


app.Run();
#endregion

#region Actions/EndPoints
void AdicionandoActionOrEndpoint(WebApplication app)
{
    #region Mapeamento de metodos para expor dados da nossa API

    // Registrar Usuário
    #region OBS IDENTITY Register
    /*
        SignInManager<IdentityUser> e UserManager<IdentityUser> userManager
         - É coisa do Identity ele setia injetado na controller mas como não temos controles eles vão entrar como parametros,o próprio .net se resolve pra passar
           essas instacias para nós usarmos.

        IOptions<AppJwtSettings>
          - É para obtermos as informações da configuração que fizemos lá no nosso appsettings.json

        RegisterUser
          - É entidade do Identity que representa o nosso usuário. Um detalhe é que a ordem que estamos passando o parametro importa, nesse caso como estamos
            utilizando o RegisterUser como parametro de entrada devemos passar ele por último.
     */
    #endregion
    app.MapPost("/registro", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser registerUser) =>
    {
        if (registerUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        // Criando o usuário
        var result = await userManager.CreateAsync(user, registerUser.Password);

        if (!result.Succeeded)
            return Results.BadRequest(result.Errors);

        // GerarToken
        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(user.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
      .Produces(StatusCodes.Status200OK)
      .Produces(StatusCodes.Status400BadRequest)
      .WithName("RegistroUsuario")
      .WithTags("Usuario");


    // Login Usuário
    #region Identity Login
    /*
         SignInManager<IdentityUser> e UserManager<IdentityUser> userManager
         - É coisa do Identity ele setia injetado na controller mas como não temos controles eles vão entrar como parametros,o próprio .net se resolve pra passar
           essas instacias para nós usarmos.

        IOptions<AppJwtSettings>
          - É para obtermos as informações da configuração que fizemos lá no nosso appsettings.json

        LoginUser
          - o Login também vai receber todo mundo injetado, a diferença é que no Register usamos a class Registeruser e aqui usamos o LoginUser, ele é um dado diferente
            porque não precisamos passar o confirm password,isso é, não precisamos passar duas vezes a senha e não exigirá que as senhas sejam iguais.

        OBS: Observamos que toda vez que precisarmos usar um objeto, precisamos passar um a um por parametro não tem como injetar eles em algum lugar e depois só fazer
             referência

     */
    #endregion
    app.MapPost("/login", [AllowAnonymous] async (
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IOptions<AppJwtSettings> appJwtSettings,
            LoginUser loginUser) => // diferente Registe
    {
        if (loginUser == null)
            return Results.BadRequest("Usuário não informado");

        if (!MiniValidator.TryValidate(loginUser, out var errors))
            return Results.ValidationProblem(errors);

        var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

        if (result.IsLockedOut)
            return Results.BadRequest("Usuário bloqueado");

        if (!result.Succeeded)
            return Results.BadRequest("Usuário ou senha inválidos");

        var jwt = new JwtBuilder()
                    .WithUserManager(userManager)
                    .WithJwtSettings(appJwtSettings.Value)
                    .WithEmail(loginUser.Email)
                    .WithJwtClaims()
                    .WithUserClaims()
                    .WithUserRoles()
                    .BuildUserResponse();

        return Results.Ok(jwt);

    }).ProducesValidationProblem()
          .Produces(StatusCodes.Status200OK)
          .Produces(StatusCodes.Status400BadRequest)
          .WithName("LoginUsuario")
          .WithTags("Usuario");




    //ObterTodos Fornecedores
    #region  MapGet  - Action/EndPoint
    /*
        O MapGet é o verbo Get

    # Como funciona? #
        O primeiro parametro é sempre a rota e o segundo parametro é um delegate que vai fazer aquilo que tem que ser feito. O que essa ação vai fazer? ela vai até
        o contexto do entity framework buscar os fornecedores, e onde injetamos o contexto do entity framework? não injetamos não é igual fazemos em uma controller
        aqui é um mapeamento de um endpoint é como se fosse um metodo isolado tudo que precisamos precisa ser passado aqui.

        app.MapGet("/fornecedor", async (
        MinimalContextDb context) =>
        await context.Fornecedores.ToListAsync())

        WithTags("Fornecedor"); => Adicionamos uma categoria no Swagger com o nome fonecedor, todos os outros metodos que estiverem configurados
        com a WithTags("Fornecedor") ficarão dentro da categoria de fornecedor


        Produces =>  Essa documentação é importante porque vamos dizer para a documentação da API(Swagger) o que essa chamada produz
            ex:
            .Produces<Fornecedor>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)

       ProducesValidationProblem() => Informa para documentação que de qualquer forma vai ser emitido um erro 400(BadRequest) só que com modelo de dados das mensagens
       que foram configuradas usando data annotation
     */
    #endregion
    app.MapGet("/fornecedor", [Authorize] async (
        MinimalContextDb context) =>
        await context.Fornecedores.ToListAsync())
        .WithName("GetFornecedor")
        .WithTags("Fornecedor");


    // Consultar Fornecedor por ID
    app.MapGet("/fornecedor/{id}", [Authorize] async (
        Guid id,
        MinimalContextDb context) =>

        await context.Fornecedores.FindAsync(id)
            #region is Fornecedor fornecedor ? 
            /*
             *  is Fornecedor fornecedor ? Results.Ok(fornecedor) : Results.NotFound())
                if Ternário, se existir o fornecedor, retornará um status ok mais o fornecedor, caso contrário(se não/Else) retornará um NotFound
             */
    #endregion
            is Fornecedor fornecedor ? Results.Ok(fornecedor) : Results.NotFound())
         #region Produces
        /*
           Essa documentação é importante porque vamos dizer para a documentação da API(Swagger) o que essa chamada produz

            .Produces<Fornecedor>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
         */
    #endregion
        .Produces<Fornecedor>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .WithName("GetFornecedorPorId")
        .WithTags("Fornecedor");


    // Add Fornecedor
    #region MapPost
    /*
       Como vamos trabalhar com uma nova entrada de dados vamos passar no parametro além do nosso Dbcontext a entidade fornecedor, lembrando que é uma boa pratica
       não expor nossas entidades de negocios e sim uma Voew Model de modelo de Entrar/Saída
     */
    #endregion
    app.MapPost("/fornecedor", [Authorize] async (
        MinimalContextDb context,
        Fornecedor fornecedor) =>
    {
        #region Validação com Mivalidation
        /*
           Para validar o a nossa entidade ou model precisamos instalar o pacote Minivalidation feito por Damian Edwards para validar a models para ser inserida
           no banco de dados. Coisa que na Controler utilizariamos o ModelState para validar as nossas models. A única coisa que ainda é parecido é a decoração
           das entides ou models usando data annotation.

           if (!MiniValidator.TryValidate(fornecedor, out var errors)) => Caso o Minivalidator não consiga validar com sucesso a entidade fornecedor os erros 
           irão para coleção de errors baseado no modelo(cada propriedade vai ter a sua data annotation que esta por sua vez vai produzir uma mensagem de erro).
           O miniValidation tem o mesmo papel da ModelState no sentindo de validar as data annotation se tivessemos trabalhando com controllers.

           Esse ValidationProblem ele retorna bad Request(400) e ele já embute lá dentro um dicionário de erros para que a nossa Api mostre exatamente os erros que
           está lá em cada propriedade assim não precisamos nos preocupar em ter que montar essa lista de erros e ter que e ter que pensar em como passar essa lista
           para o client.

           Essa biblioteca é o que temos hj no momento, caso a gente não queira usa-lá. Podemos montar essa validação na mão usando ifs por exemplo
         */
        #endregion
        if (!MiniValidator.TryValidate(fornecedor, out var errors))
            return Results.ValidationProblem(errors);

        var fornecedorbanco = await context.Fornecedores.FindAsync(fornecedor.id);

        if (fornecedorbanco != null) return Results.BadRequest("Já existe um fornecedor Cadastrado para esse Guid");

        context.Fornecedores.Add(fornecedor);
        var result = await context.SaveChangesAsync();

        #region IF Ternário
        /*
           If Ternario irá retornar como result o fornecedor cadastrado caso a requisição tenha sido salvo no banco com sucesso, caso contrário irá retonrar um
           BadRequest com a seguinte mensagem  Houve um problema ao salvar o registro
         */
        #endregion
        return result > 0
            //? Results.Created($"/fornecedor/{fornecedor.id}", fornecedor) - Mesma coisa do que a linha de baixo. 
            ? Results.CreatedAtRoute("GetFornecedorPorId", new { id = fornecedor.id }, fornecedor)
            : Results.BadRequest("Houve um problema ao salvar o registro");
    })
    #region ProducesValidationProblem()
       /*
            Informa para documentação que de qualquer forma vai ser emitido um erro 400(BadRequest) só que com modelo de dados das mensagens  que foram
            configuradas usando data annotation
        */
    #endregion
       .ProducesValidationProblem()
       .Produces<Fornecedor>(StatusCodes.Status201Created)
       .Produces(StatusCodes.Status400BadRequest)
       .WithName("PostFornecedor")
       .WithTags("Fornecedor");



    // Atualizar Fornecedor
    app.MapPut("/fornecedor/{id}", [Authorize] async (Guid id, MinimalContextDb context, Fornecedor fornecedor) =>
    {
        //var fornecedorBanco = await context.Fornecedores.FindAsync(id);

        var fornecedorBanco = await context.Fornecedores.AsNoTracking<Fornecedor>()
                                                    .FirstOrDefaultAsync(f => f.id == id);

        if (fornecedorBanco == null) return Results.NotFound();

        if (!MiniValidator.TryValidate(fornecedor, out var errors))
            return Results.ValidationProblem(errors);

        context.Fornecedores.Update(fornecedor);
        var result = await context.SaveChangesAsync();

        return result > 0
        ? Results.NoContent()
        : Results.BadRequest("Houve um problema ao salvar o registro");
    })
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .WithName("PutFornecedor")
        .WithTags("Fornecedor");



    // Delete Fornecedor
    app.MapDelete("/fornecedor/{id}", [Authorize] async (Guid id, MinimalContextDb context) =>
    {
        var fornecedor = await context.Fornecedores.FindAsync(id);
        if (fornecedor == null) return Results.NotFound();

        context.Fornecedores.Remove(fornecedor);
        var result = await context.SaveChangesAsync();

        return result > 0
        ? Results.NoContent()
        : Results.BadRequest("Houve um problema ao salvar o registro");
    })
        .ProducesValidationProblem()
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization("ExcluirFornecedor")
        .WithName("DeleteFornecedor")
        .WithTags("Fornecedor");
    #endregion
}
#endregion

