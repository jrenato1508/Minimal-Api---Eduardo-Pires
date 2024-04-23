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

#region Configura��o Identity com o JWT token
#region OBS
/*
    Pessoal, quem estiver estudando (hoje) esse material tem que adicionar a biblioteca NetDevPack.Identity na vers�o 6.0.5 por que na �ltima vers�o
    acaba gerado erro.

    Adicionamos dois servi�os o AddIdentityEntityFrameworkContextConfiguration e tanb�m o AddIdentityConfiguration
    
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
     Configura��o para usarmos Claim atrav�s das Policys. Estamos adicionando uma Policy com o nome ExcluirFornecedor que por usa vez requer uma Claim com
     o nome de ExcluirFornecedor.
     Agora para configurar a action/ Endpoint para exigir que al�m autentica��o o usu�rio precisa ter determinada claim, adicionamos essa exigencia no metadado
     com a seguinte	informa��o .RequireAuthorization("ExcluirFornecedor") Vamos adicionar essa informa��o na action Map.Delete
	ex:
			.Produces(StatusCodes.Status400BadRequest)
			.Produces(StatusCodes.Status204NoContent)
			.Produces(StatusCodes.Status404NotFound)
			.RequireAuthorization("ExcluirFornecedor")
			.WithName("DeleteFornecedor")
			.WithTags("Fornecedor");

     Agora vamos atribuir essa claim ao usu�rio que criamos, para isso basta pegar o Guid do usu�rio na tabela AspNetUsers e adicionar uma claim na
     tabela AspNetUserClaims com o claimType e ClaimValue como ExcluirFornecedor. O claim value n�o precisa mas vamos preencher de qualquer forma. Feito isso,
     podemos testar se o token que � gerado ao realizar o login contem as claim que adicionamos ao usu�rio, parra isso basta pegar o token e jogar no site
    jwt pra analisarmos.
 */
#endregion
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ExcluirFornecedor",
        policy => policy.RequireClaim("ExcluirFornecedor"));
});


builder.Services.AddEndpointsApiExplorer();

#region Configura��o do Swagger
//builder.Services.AddSwaggerGen(); sai disso aqui para o trecho de c�digo a seguir
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Minimal API Sample",
        Description = "Desenvolvido por Jos� Renato - Orientado Por Eduardo Piress ",
        Contact = new OpenApiContact { Name = "Jos� Renato", Email = "jrenato1508@gmail.com.net.br" },
        License = new OpenApiLicense { Name = "MIT", Url = new Uri("https://opensource.org/licenses/MIT") }
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "Insira o token JWT desta maneira: Bearer + espa�o + seu token",
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
  Do var app = builder.Build(); pra baixo � a configura��o do fluxo do request
  var app = builder.Build(); // constru��o da nossa api com todos os servi��es que foram adicionados na pipeline
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

    // Registrar Usu�rio
    #region OBS IDENTITY Register
    /*
        SignInManager<IdentityUser> e UserManager<IdentityUser> userManager
         - � coisa do Identity ele setia injetado na controller mas como n�o temos controles eles v�o entrar como parametros,o pr�prio .net se resolve pra passar
           essas instacias para n�s usarmos.

        IOptions<AppJwtSettings>
          - � para obtermos as informa��es da configura��o que fizemos l� no nosso appsettings.json

        RegisterUser
          - � entidade do Identity que representa o nosso usu�rio. Um detalhe � que a ordem que estamos passando o parametro importa, nesse caso como estamos
            utilizando o RegisterUser como parametro de entrada devemos passar ele por �ltimo.
     */
    #endregion
    app.MapPost("/registro", [AllowAnonymous] async (
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IOptions<AppJwtSettings> appJwtSettings,
        RegisterUser registerUser) =>
    {
        if (registerUser == null)
            return Results.BadRequest("Usu�rio n�o informado");

        if (!MiniValidator.TryValidate(registerUser, out var errors))
            return Results.ValidationProblem(errors);

        var user = new IdentityUser
        {
            UserName = registerUser.Email,
            Email = registerUser.Email,
            EmailConfirmed = true
        };

        // Criando o usu�rio
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


    // Login Usu�rio
    #region Identity Login
    /*
         SignInManager<IdentityUser> e UserManager<IdentityUser> userManager
         - � coisa do Identity ele setia injetado na controller mas como n�o temos controles eles v�o entrar como parametros,o pr�prio .net se resolve pra passar
           essas instacias para n�s usarmos.

        IOptions<AppJwtSettings>
          - � para obtermos as informa��es da configura��o que fizemos l� no nosso appsettings.json

        LoginUser
          - o Login tamb�m vai receber todo mundo injetado, a diferen�a � que no Register usamos a class Registeruser e aqui usamos o LoginUser, ele � um dado diferente
            porque n�o precisamos passar o confirm password,isso �, n�o precisamos passar duas vezes a senha e n�o exigir� que as senhas sejam iguais.

        OBS: Observamos que toda vez que precisarmos usar um objeto, precisamos passar um a um por parametro n�o tem como injetar eles em algum lugar e depois s� fazer
             refer�ncia

     */
    #endregion
    app.MapPost("/login", [AllowAnonymous] async (
            SignInManager<IdentityUser> signInManager,
            UserManager<IdentityUser> userManager,
            IOptions<AppJwtSettings> appJwtSettings,
            LoginUser loginUser) => // diferente Registe
    {
        if (loginUser == null)
            return Results.BadRequest("Usu�rio n�o informado");

        if (!MiniValidator.TryValidate(loginUser, out var errors))
            return Results.ValidationProblem(errors);

        var result = await signInManager.PasswordSignInAsync(loginUser.Email, loginUser.Password, false, true);

        if (result.IsLockedOut)
            return Results.BadRequest("Usu�rio bloqueado");

        if (!result.Succeeded)
            return Results.BadRequest("Usu�rio ou senha inv�lidos");

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
        O MapGet � o verbo Get

    # Como funciona? #
        O primeiro parametro � sempre a rota e o segundo parametro � um delegate que vai fazer aquilo que tem que ser feito. O que essa a��o vai fazer? ela vai at�
        o contexto do entity framework buscar os fornecedores, e onde injetamos o contexto do entity framework? n�o injetamos n�o � igual fazemos em uma controller
        aqui � um mapeamento de um endpoint � como se fosse um metodo isolado tudo que precisamos precisa ser passado aqui.

        app.MapGet("/fornecedor", async (
        MinimalContextDb context) =>
        await context.Fornecedores.ToListAsync())

        WithTags("Fornecedor"); => Adicionamos uma categoria no Swagger com o nome fonecedor, todos os outros metodos que estiverem configurados
        com a WithTags("Fornecedor") ficar�o dentro da categoria de fornecedor


        Produces =>  Essa documenta��o � importante porque vamos dizer para a documenta��o da API(Swagger) o que essa chamada produz
            ex:
            .Produces<Fornecedor>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)

       ProducesValidationProblem() => Informa para documenta��o que de qualquer forma vai ser emitido um erro 400(BadRequest) s� que com modelo de dados das mensagens
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
                if Tern�rio, se existir o fornecedor, retornar� um status ok mais o fornecedor, caso contr�rio(se n�o/Else) retornar� um NotFound
             */
    #endregion
            is Fornecedor fornecedor ? Results.Ok(fornecedor) : Results.NotFound())
         #region Produces
        /*
           Essa documenta��o � importante porque vamos dizer para a documenta��o da API(Swagger) o que essa chamada produz

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
       Como vamos trabalhar com uma nova entrada de dados vamos passar no parametro al�m do nosso Dbcontext a entidade fornecedor, lembrando que � uma boa pratica
       n�o expor nossas entidades de negocios e sim uma Voew Model de modelo de Entrar/Sa�da
     */
    #endregion
    app.MapPost("/fornecedor", [Authorize] async (
        MinimalContextDb context,
        Fornecedor fornecedor) =>
    {
        #region Valida��o com Mivalidation
        /*
           Para validar o a nossa entidade ou model precisamos instalar o pacote Minivalidation feito por Damian Edwards para validar a models para ser inserida
           no banco de dados. Coisa que na Controler utilizariamos o ModelState para validar as nossas models. A �nica coisa que ainda � parecido � a decora��o
           das entides ou models usando data annotation.

           if (!MiniValidator.TryValidate(fornecedor, out var errors)) => Caso o Minivalidator n�o consiga validar com sucesso a entidade fornecedor os erros 
           ir�o para cole��o de errors baseado no modelo(cada propriedade vai ter a sua data annotation que esta por sua vez vai produzir uma mensagem de erro).
           O miniValidation tem o mesmo papel da ModelState no sentindo de validar as data annotation se tivessemos trabalhando com controllers.

           Esse ValidationProblem ele retorna bad Request(400) e ele j� embute l� dentro um dicion�rio de erros para que a nossa Api mostre exatamente os erros que
           est� l� em cada propriedade assim n�o precisamos nos preocupar em ter que montar essa lista de erros e ter que e ter que pensar em como passar essa lista
           para o client.

           Essa biblioteca � o que temos hj no momento, caso a gente n�o queira usa-l�. Podemos montar essa valida��o na m�o usando ifs por exemplo
         */
        #endregion
        if (!MiniValidator.TryValidate(fornecedor, out var errors))
            return Results.ValidationProblem(errors);

        var fornecedorbanco = await context.Fornecedores.FindAsync(fornecedor.id);

        if (fornecedorbanco != null) return Results.BadRequest("J� existe um fornecedor Cadastrado para esse Guid");

        context.Fornecedores.Add(fornecedor);
        var result = await context.SaveChangesAsync();

        #region IF Tern�rio
        /*
           If Ternario ir� retornar como result o fornecedor cadastrado caso a requisi��o tenha sido salvo no banco com sucesso, caso contr�rio ir� retonrar um
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
            Informa para documenta��o que de qualquer forma vai ser emitido um erro 400(BadRequest) s� que com modelo de dados das mensagens  que foram
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

