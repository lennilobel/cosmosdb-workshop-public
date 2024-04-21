function udfCalculatePremium(amount, country, state) {
	if (country !== 'United States') {
		amount = amount * 1.3;	// Premium charge is 30% anywhere outside the U.S.
	}
	else if (state == 'New York' || state == 'New Jersey' || state == 'Connecticut') {
		amount = amount * 1.2;	// Premium charge is 20% anywhere in NY, NJ, or CT
	}
	return amount;
}
