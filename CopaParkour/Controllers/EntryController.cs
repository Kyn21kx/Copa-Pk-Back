using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CopaParkour.Models;
using Firebase.Database;
using Firebase.Database.Query;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.ComponentModel.DataAnnotations;
using QRCoder;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Cors;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace CopaParkour.Controllers;

[ApiController]
[Route("entry")]
[EnableCors("_allowAllOrigins")]
public class EntryController : ControllerBase
{
	const string FIREBASE_URL = "https://copa-pk-default-rtdb.firebaseio.com/";
	const string AUTH = "AIzaSyC5fvRptJM1vTVKPYNUnYI0Dz1L9vB7y28";
	const string APP_ID = "1:103103009517:web:783c9b318e1e89ff72f2e4";
	const string USERS_TABLE = "Users";
	const string INVALID_PHONE_MSG = "El número de teléfono ingresado no es válido!";
    const string ALREADY_EXISTING_EMAIL = "El correo ingresado ya existe, favor de revisar su aplicación!";
    const string INVALID_CATEGORY = "Su categoría no existe o no es válida, favor de revisar su aplicación!";
	const string RESOURCES_PATH = "wwwroot";
	const string CARTA_MENORES_PATH = $"{RESOURCES_PATH}/CARTA_MENORES.pdf";
	const string CARTA_ADULTOS_PATH = $"{RESOURCES_PATH}/CARTA_ADULTOS.pdf";

    FirebaseClient client;

	public EntryController()
	{
		this.client = new FirebaseClient(FIREBASE_URL);
	}

	[HttpGet]
	public IActionResult Test()
	{
		return Ok();
	}

	[HttpPost]
	public async Task<FileContentResult> SendEntry([FromBody] EntryDto input)
	{
		//Insert that to our database (this can be anywhere tbh)

		//Create a QR code that will be sent out
		//Validate the input
		var entities = await this.client.Child(USERS_TABLE).OnceAsync<EntryDto>();
		var list = entities.AsQueryable().Select((x) => x.Object.Email);
		EntryInputResult validationResult = ValidateEntry(in input, list);
		//Check and throw
		CheckErrors(validationResult);
		await this.client.Child(USERS_TABLE).PostAsync(input);
		//Generate our QR
        var qrCodeImage = GenerateEntryCode(input).GetGraphic(3);
		Stream pdfData = null;
		if (input.Category == Categories.Infantil)
		{
			pdfData = this.AppendImageToPDF(CARTA_MENORES_PATH, qrCodeImage);
		}
		else
		{
            pdfData = this.AppendImageToPDF(CARTA_ADULTOS_PATH, qrCodeImage);
        }
		try
		{
			var bytes = pdfData.ReadAllBytes();
			pdfData.Flush();
			pdfData.Close();
            var result = File(bytes, "application/pdf", $"Carta_Responsiva_{input.Name}.pdf");
			return result;
        }
		catch (Exception err)
		{
			Console.WriteLine(err);
			throw err;
		}
    }

	private EntryInputResult ValidateEntry(in EntryDto input, IQueryable<string?> allEmails)
	{
		const int PHONE_LENGTH = 10;
		if (input.Phone.Length != PHONE_LENGTH)
		{
			return EntryInputResult.InvalidPhone;
		}

		if (input.Category > Categories.Libre || input.Category < 0)
		{
			return EntryInputResult.InvalidCategory;
		}
		if (allEmails.Contains(input.Email.ToLower()))
		{
			return EntryInputResult.AlreadyExistingEmail;
		}
		return EntryInputResult.ValidInput;
    }

	private void CheckErrors(EntryInputResult validationResult)
	{
        switch (validationResult)
        {
            case EntryInputResult.AlreadyExistingEmail:
                throw new HttpRequestException(ALREADY_EXISTING_EMAIL, null, HttpStatusCode.Conflict);
            case EntryInputResult.InvalidCategory:
				throw new HttpRequestException(INVALID_CATEGORY, null, HttpStatusCode.BadRequest);
            case EntryInputResult.InvalidPhone:
                throw new HttpRequestException(INVALID_PHONE_MSG, null, HttpStatusCode.BadRequest);
            case EntryInputResult.ValidInput:
                return;
        }
    }

	private PngByteQRCode GenerateEntryCode(EntryDto input)
	{
        QRCodeGenerator qrGenerator = new QRCodeGenerator();
        QRCodeData qrCodeData = qrGenerator.CreateQrCode(input.ToString(), QRCodeGenerator.ECCLevel.Q);
        return new PngByteQRCode(qrCodeData);
    }

	private Stream AppendImageToPDF(string pdf, byte[] imageData)
	{
		using (Stream inputPdfStream = new FileStream(pdf, FileMode.Open, FileAccess.Read, FileShare.Read))
		using (Stream inputImageStream = new MemoryStream(imageData))
		{
			Stream outputPdfStream = new MemoryStream((int)(inputPdfStream.Length + inputImageStream.Length));

            var reader = new PdfReader(inputPdfStream);
			var stamper = new PdfStamper(reader, outputPdfStream);
			var pdfContentByte = stamper.GetOverContent(1);

			iTextSharp.text.Image image = iTextSharp.text.Image.GetInstance(inputImageStream);
            image.SetAbsolutePosition(225, 10);
            pdfContentByte.AddImage(image);
			stamper.Close();
			return outputPdfStream;
        }
    }
}

