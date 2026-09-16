extends RefCounted
## Asset-only palette; independent of player classes and autoloads.
const COLORS := {
	"Cyber Shogun": Color(0.86, 0.96, 1.0),
	"Solaria": Color(1.0, 0.08, 0.65),
	"Oceanus": Color(0.02, 0.85, 1.0),
	"Surya Prime": Color(1.0, 0.65, 0.06),
	"Mirage Blade": Color(1.0, 0.97, 0.12),
	"Apex Titan": Color(1.0, 0.25, 0.025),
}

static func color_for(operative: String) -> Color:
	return COLORS.get(operative, Color(0.02, 0.85, 1.0))
