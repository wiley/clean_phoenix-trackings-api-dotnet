using Trackings.API.Responses;
using Trackings.API.Responses.NonSuccessfullResponses;
using Trackings.Domain.Trackings;
using Trackings.Domain.Exceptions;
using Trackings.Domain.Utils.CustomValidations;
using Trackings.Services.Interfaces;
using DarwinAuthorization.Models;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Threading.Tasks;
using WLS.Log.LoggerTransactionPattern;
using Trackings.API.Requests;
using Newtonsoft.Json;
using MongoDB.Bson;
using Newtonsoft.Json.Linq;
using Trackings.API.Helpers.Converters;
using Trackings.Domain.Pagination;
using Trackings.Domain.Utils;

namespace Trackings.API.Controllers
{
    [Route("api/v{version:apiVersion}/trackings")]
    [Consumes("application/json")]
    [Produces("application/json")]
    [ApiVersion("4.0")]
    [ApiController]
    public class TrackingsController : ControllerBase
    {
        private readonly ILogger<TrackingsController> _logger;
        private readonly ILoggerStateFactory _loggerStateFactory;
        private readonly ITrackingsService _service;
        private readonly DarwinAuthorizationContext _darwinAuthorizationContext;
        private readonly Mapper _mapper_response;
        private readonly Mapper _mapper_request;

        public TrackingsController(ILogger<TrackingsController> logger, ILoggerStateFactory loggerStateFactory, ITrackingsService service, DarwinAuthorizationContext darwinAuthorizationContext)
        {
            _loggerStateFactory = loggerStateFactory;
            _logger = logger;
            _service = service;
            _darwinAuthorizationContext = darwinAuthorizationContext;

            _mapper_request = new Mapper(new MapperConfiguration(cfg => {
                cfg.CreateMap<TrackingsCreateOrUpdateRequest, Tracking>();
                cfg.CreateMap<JObject, BsonDocument>().ConvertUsing<JObjectToBsonDocumentConverter>();
                cfg.AllowNullCollections = true;
            }));
            _mapper_response = new Mapper(new MapperConfiguration(cfg => {
                cfg.CreateMap<Tracking, TrackingsResponse>();
                cfg.CreateMap<TrackingsContext, TrackingsContextResponse>();
                cfg.CreateMap<BsonDocument, JObject>().ConvertUsing<BsonDocumentToJObjectConverter>();

            }));
        }

        [HttpPut("generate-kafka-events")]
        [Authorize]
        [ProducesResponseType(202)]
        [ProducesResponseType(503)]
        public IActionResult GenerateKafkaEvents()
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    _service.GenerateKafkaEvents();
                    return Accepted();
                }
                catch (GenerateKafkaEventsAlreadyRunningException)
                {
                    return StatusCode(503, "Another process is already running");
                }
            }
        }


        [HttpGet("{TrackingId}", Name = "GetTracking")]
        [Authorize]
        [ProducesResponseType(typeof(TrackingsResponse), 200)]
        [ProducesResponseType(typeof(BadRequestMessage), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ResourceNotFoundMessage), 404)]
        [ProducesResponseType(500)]
        public ActionResult GetTracking
            (
            [Required]
            [CustomIdFormatValidation]
            [FromRoute] Guid TrackingId
        )
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("GetTracking - Bad Request - {Id}", TrackingId);
                        return BadRequest(NonSuccessfullRequestMessageFormatter.FormatBadRequestResponse(ModelState));
                    }

                    var trackings = _service.GetTrackingData(TrackingId);
                    if (trackings == null)
                    {
                        _logger.LogWarning("GetTracking - Not Found - {Id}", TrackingId);
                        return NotFound(NonSuccessfullRequestMessageFormatter.FormatResourceNotFoundResponse());
                    }

                    TrackingsResponse response = _mapper_response.Map<TrackingsResponse>(trackings);
                    response._links.Self.Href = Url.Link("GetTracking", new { Id = response.Id.ToString() });
                    return Ok(response);
                }
                catch (NotFoundException e)
                {
                    _logger.LogWarning(e, "GetTracking - NotFound - {Id}", TrackingId);
                    return NotFound(NonSuccessfullRequestMessageFormatter.FormatResourceNotFoundResponse());
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "GetTracking - Unhandled Exception");
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(201)]
        [ProducesResponseType(typeof(BadRequestMessage), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> CreateTracking(
            [FromBody] TrackingsCreateOrUpdateRequest request
        )
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("CreateTracking - Bad Request");
                        return BadRequest(NonSuccessfullRequestMessageFormatter.FormatBadRequestResponse(ModelState));
                    }

                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("CreateTracking - Bad Request");
                        return BadRequest(NonSuccessfullRequestMessageFormatter.FormatBadRequestResponse(ModelState));
                    }
                    var tracking = _mapper_request.Map<Tracking>(request);
                    await _service.InsertTrackingData(tracking);
                    TrackingsResponse response = _mapper_response.Map<TrackingsResponse>(tracking);
                    response._links.Self.Href = Url.Link("GetTracking", new { TrackingId = response.Id });

                    var routeValues = new { TrackingId = response.Id };
                    return CreatedAtRoute("GetTracking", routeValues, response);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "CreateTracking - Unhandled Exception");
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpPut("{TrackingId}")]
        [Authorize]
        [ProducesResponseType(typeof(TrackingsResponse), 200)]
        [ProducesResponseType(typeof(BadRequestMessage), 400)]
        [ProducesResponseType(typeof(ResourceNotFoundMessage), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> UpdateTracking([Required]
            [CustomIdFormatValidation]
            [FromRoute] Guid TrackingId, 
            [FromBody] TrackingsCreateOrUpdateRequest request
        )
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("UpdateTracking - Bad Request");
                        return BadRequest(NonSuccessfullRequestMessageFormatter.FormatBadRequestResponse(ModelState));
                    }

                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("UpdateTracking - Bad Request");
                        return BadRequest(NonSuccessfullRequestMessageFormatter.FormatBadRequestResponse(ModelState));
                    }

                    Tracking tracking = _mapper_request.Map<Tracking>(request);

                    tracking = await _service.UpdateTrackingData(TrackingId, tracking);

                    TrackingsResponse response = _mapper_response.Map<TrackingsResponse>(tracking);
                    response._links.Self.Href = Url.Link("GetTracking", new { TrackingId = response.Id });

                    var routeValues = new { response.Id };
                    return Ok(response);
                }
                catch (NotFoundException e)
                {
                    _logger.LogWarning(e, "UpdateTracking - NotFound - {Id}", TrackingId);
                    return NotFound(NonSuccessfullRequestMessageFormatter.FormatResourceNotFoundResponse());
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "UpdateTracking - Unhandled Exception");
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }

        [HttpDelete("{TrackingId}")]
        [Authorize]
        [ProducesResponseType(204)]
        [ProducesResponseType(typeof(BadRequestMessage), 400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(typeof(ResourceNotFoundMessage), 404)]
        [ProducesResponseType(500)]
        public async Task<IActionResult> DeleteTracking(
            [Required]
            [CustomIdFormatValidation]
            [FromRoute] Guid TrackingId
        )
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
                try
                {
                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("DeleteTracking - Bad Request - {Id}", TrackingId);
                        return BadRequest(NonSuccessfullRequestMessageFormatter.FormatBadRequestResponse(ModelState));
                    }

                    await _service.DeleteTrackingData(TrackingId);

                    return NoContent();
                }
                catch (NotFoundException)
                {
                    _logger.LogWarning("DeleteTracking - Not Found - {Id}", TrackingId);
                    return NotFound(NonSuccessfullRequestMessageFormatter.FormatResourceNotFoundResponse());
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "DeleteTracking - Unhandled Exception");
                    return StatusCode(500);
                }
        }

        [HttpPost]
        [Authorize]
        [ProducesResponseType(typeof(ReturnOutput<TrackingsResponse>), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(500)]
        [Route("Search")]
        public async Task<IActionResult> SearchTracking([FromQuery] string include, [FromBody] TrackingsSearchRequest request)
        {
            using (_logger.BeginScope(_loggerStateFactory.Create(Request.Headers["Transaction-ID"])))
            {
                try
                {
                    if (!string.IsNullOrEmpty(include) && !include.Equals("data"))
                    {
                        ModelState.AddModelError("include", "Wrong value");
                    }

                    if (!ModelState.IsValid)
                    {
                        _logger.LogWarning("SearchTracking - Bad Request");
                        return BadRequest(NonSuccessfullRequestMessageFormatter.FormatBadRequestResponse(ModelState));
                    }

                    bool includeData = !string.IsNullOrEmpty(include) && include.Equals("data") ? true : false;

                    List<Tracking> trackings = await _service.SearchTrackings(request, includeData);
                    List<TrackingsResponse> response = trackings.ConvertAll(tracking => _mapper_response.Map<TrackingsResponse>(tracking));
                    response.ForEach(tracking => tracking._links.Self.Href = Url.Link("GetTracking", new { TrackingId = tracking.Id.ToString() }));

                    ReturnOutput<TrackingsResponse> formattedEntitlement = new ReturnOutput<TrackingsResponse>();
                    formattedEntitlement.Items = response;
                    formattedEntitlement.Count = response.Count;

                    return Ok(formattedEntitlement);
                }
                catch (BadRequestException exception)
                {
                    _logger.LogWarning(exception, "SearchTracking - Unhandled Exception");
                    return BadRequest();
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "SearchTracking - Unhandled Exception");
                    return StatusCode((int)HttpStatusCode.InternalServerError);
                }
            }
        }
    }
}