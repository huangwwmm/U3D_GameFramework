local gflog = {}

function gflog.log(tag, msg)
	gfloginternal.log(tag, debug.traceback(msg, 2))
end

function gflog.warning(tag, msg)
	gfloginternal.warning(tag, debug.traceback(msg, 2))
end

function gflog.error(tag, msg)
	gfloginternal.error(tag, debug.traceback(msg, 2))
end

function gflog.verbose(tag, msg)
	gfloginternal.verbose(tag, debug.traceback(msg, 2))
end

return gflog