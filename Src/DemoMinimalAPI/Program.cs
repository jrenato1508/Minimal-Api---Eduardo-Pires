using DemoMinimalAPI.Data;
using DemoMinimalAPI.Models;
using Microsoft.EntityFrameworkCore;
using MiniValidation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<MinimalContextDb>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));



builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

#region OBS
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

app.UseHttpsRedirection();

/*
 Pessoal, quem estiver estudando (hoje) esse material tem que adicionar a biblioteca NetDevPack.Identity na vers�o 6.0.5 por que na �ltima vers�o
 acaba gerado erro.
 */

#region Mapeamento de metodos para expor dados da nossa API

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
app.MapGet("/fornecedor", async (
    MinimalContextDb context) =>
    await context.Fornecedores.ToListAsync())
    .WithName("GetFornecedor")
    .WithTags("Fornecedor");


// Consultar Fornecedor por ID
app.MapGet("/fornecedor/{id}", async (
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
app.MapPost("/fornecedor", async (
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
app.MapPut("/fornecedor/{id}", async (Guid id, MinimalContextDb context, Fornecedor fornecedor) =>
{
    var fornecedorBanco = await context.Fornecedores.FindAsync(id);
    if (fornecedorBanco == null) return Results.NotFound();

    if (!MiniValidator.TryValidate(fornecedor, out var errors))
        return Results.ValidationProblem(errors);

    context.Fornecedores.Update(fornecedorBanco);
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
app.MapDelete("/fornecedor/{id}", async (Guid id, MinimalContextDb context) =>
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
    .WithName("DeleteFornecedor")
    .WithTags("Fornecedor");
#endregion

app.Run();


// Parei no minuto 48 - Teste do CRUD no Swagger