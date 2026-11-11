import props
# Hook for double-density (x2) props. The imported tree variants were REMOVED on Yan's word (2026-11-10: "the trees do not fit at all, remove them,
# everything from wonki son"). What it installs now: props_x2.py.
try:
    import props_x2
    props.PROPS.update(props_x2.ALL); props.VARIANTS.update(props_x2.ALL_VARIANTS)      # the x2 redraws of 2026-11-11 (furniture in every facing, the car wreck, the lathe, ...); furn_x2.py is the superseded first try
except ImportError:
    pass
