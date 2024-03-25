using AuctionService.Data;
using AuctionService.DTOs;
using AuctionService.Entities;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuctionService.Controllers;

[ApiController]
[Route("api/auctions")]
public class AuctionsController : ControllerBase
{
	private readonly AuctionDbContext _context;
	private readonly IMapper _mapper;

	public AuctionsController(AuctionDbContext context, IMapper mapper)
	{
		_context = context;
		_mapper = mapper;
	}

	[HttpGet]
	public async Task<ActionResult<List<AuctionDto>>> GetAllAuctions()
	{
		var auctions = await _context.Auctions
			.Include(x => x.Item)
			.OrderBy(x => x.Item.Make)
			.ToListAsync();

		return _mapper.Map<List<AuctionDto>>(auctions);
	}

	[HttpGet("{id}")]
	public async Task<ActionResult<AuctionDto>> GetAuctionById(Guid id)
	{
		var auction = await _context.Auctions
			.Include(x => x.Item)
			.FirstOrDefaultAsync(x => x.Id == id);

		if (auction == null) return NotFound();

		return _mapper.Map<AuctionDto>(auction);
	}

	[HttpPost]
	public async Task<ActionResult<AuctionDto>> CreateAuction(CreateAuctionDto auctionDto)
	{
		var auction = _mapper.Map<Auction>(auctionDto);
		// TODO: add current user as seller.
		auction.Seller = "test";

		// Nothing added to db yet, it is just tracking it in memory
		_context.Auctions.Add(auction);

		// Zero because this save changes async method returns an int for each change it was able to save in the db.
		var result = await _context.SaveChangesAsync() > 0;

		if (!result) return BadRequest("Could not save changes to the DB");

		return CreatedAtAction(nameof(GetAuctionById), new { auction.Id }, _mapper.Map<AuctionDto>(auction));
	}

	[HttpPut("{id}")]
	public async Task<ActionResult> UpdateAuction(Guid id, UpdateAuctionDto updateAuctionDto)
	{
		var auction = await _context.Auctions.Include(x => x.Item).FirstOrDefaultAsync(x => x.Id == id);

		if (auction == null) return NotFound();

		// TODO: check seller == username;

		// Up for debate whether you'd allow users to update the make and all that after an auction has started.
		auction.Item.Make = updateAuctionDto.Make ?? auction.Item.Make;
		auction.Item.Model = updateAuctionDto.Model ?? auction.Item.Model;
		auction.Item.Color = updateAuctionDto.Color ?? auction.Item.Color;
		auction.Item.Mileage = updateAuctionDto.Mileage ?? auction.Item.Mileage;
		auction.Item.Year = updateAuctionDto.Year ?? auction.Item.Year;

		var result = await _context.SaveChangesAsync() > 0;

		if (result) return Ok();

		return BadRequest("Problem saving changes");
	}

	/*
		Again, you probably don't want to allow users to delete an auction that is
		already live in real life.
		Perhaps it should be allowed if for example, there are no bids yet.
		But if someone sets their reserve price at 1, and someone bids 1, they
		should not be allowed to delete it...
	*/
	[HttpDelete("{id}")]
	public async Task<ActionResult> DeleteAuction(Guid id)
	{
		var auction = await _context.Auctions.FindAsync(id);

		if (auction == null) return NotFound();

		// TODO: seller = usename

		_context.Auctions.Remove(auction);

		var result = await _context.SaveChangesAsync() > 0;

		if (!result) return BadRequest("Could not update DB");

		return Ok();
	}
}
