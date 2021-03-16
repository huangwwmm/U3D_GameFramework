#pragma once

#include <stdlib.h>
#include <string.h>
#include "lua.h"
#include "lauxlib.h"
#include "lobject.h"

#define SIZET_LENGTH sizeof(size_t)

#define TYPE_LOG 3
#define TYPE_WARING 2
#define TYPE_ERROR 0
#define TYPE_VERBOSE 100