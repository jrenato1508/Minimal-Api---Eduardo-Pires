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

app.UseHttpsRedirection();

/*
 Pessoal, quem estiver estudando (hoje) esse material tem que adicionar a biblioteca NetDevPack.Identity na versão 6.0.5 por que na última versão
 acaba gerado erro.
 */

#region Mapeamento de metodos para expor dados da nossa API

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
app.MapPost("/fornecedor", async (
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