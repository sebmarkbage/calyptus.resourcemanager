String.implement({

	padLeft: function(character, length) {
		if(this.length >= length) return this;
		return (this + character).padLeft(character, length);
	},
	
	padRight: function(character, length) {
		if(this.length >= length) return this;
		return (character + this).padRight(character, length);
	}
});