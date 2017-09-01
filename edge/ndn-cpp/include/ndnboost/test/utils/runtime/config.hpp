//  (C) Copyright Gennadiy Rozental 2005-2008.
//  Distributed under the Boost Software License, Version 1.0.
//  (See accompanying file LICENSE_1_0.txt or copy at 
//  http://www.boost.org/LICENSE_1_0.txt)

//  See http://www.boost.org/libs/test for the library home page.
//
//  File        : $RCSfile$
//
//  Version     : $Revision$
//
//  Description : Runtime.Param library configuration
// ***************************************************************************

#ifndef NDNBOOST_RT_CONFIG_HPP_062604GER
#define NDNBOOST_RT_CONFIG_HPP_062604GER

// Boost
#include <ndnboost/config.hpp>
#ifdef NDNBOOST_MSVC
# pragma warning(disable: 4511) // copy constructor could not be generated
# pragma warning(disable: 4512) // assignment operator could not be generated
# pragma warning(disable: 4181) // qualifier applied to reference type; ignored
# pragma warning(disable: 4675) // resolved overload was found by argument-dependent lookup
#endif

// Boost.Test
#include <ndnboost/test/detail/config.hpp>
#include <ndnboost/test/utils/basic_cstring/basic_cstring.hpp>
#include <ndnboost/test/utils/wrap_stringstream.hpp>
#include <ndnboost/test/utils/basic_cstring/io.hpp> // operator<<(ndnboost::runtime::cstring)

// STL
#include <string>
#include <cstdlib>

//____________________________________________________________________________//

#ifndef NDNBOOST_RT_PARAM_CUSTOM_STRING
#  ifndef NDNBOOST_RT_PARAM_WIDE_STRING
#    define NDNBOOST_RT_PARAM_NAMESPACE                            runtime
#  else
#    define NDNBOOST_RT_PARAM_NAMESPACE                            wide_runtime
#  endif
#endif

#ifdef __SUNPRO_CC
extern int putenv(char*);
#endif

namespace ndnboost {

namespace NDNBOOST_RT_PARAM_NAMESPACE {

#ifndef NDNBOOST_RT_PARAM_CUSTOM_STRING
#  ifndef NDNBOOST_RT_PARAM_WIDE_STRING

typedef char                                                    char_type;
typedef std::string                                             dstring;
typedef unit_test::const_string                                 cstring;
typedef unit_test::literal_string                               literal_cstring;
typedef wrap_stringstream                                       format_stream;

#ifdef NDNBOOST_CLASSIC_IOSTREAMS
typedef std::ostream                                            out_stream;
#else
typedef std::basic_ostream<char_type>                           out_stream;
#endif

#ifdef NDNBOOST_MSVC
#pragma warning(push)
#pragma warning(disable:4996) // putenv
#endif

#ifndef UNDER_CE
#if defined(__COMO__) && 0
inline void
putenv_impl( cstring name, cstring value )
{
    using namespace std;
    // !! this may actually fail. What should we do?
    setenv( name.begin(), value.begin(), 1 );
}
#else
inline void
putenv_impl( cstring name, cstring value )
{
    format_stream fs;

    fs << name << '=' << value;

    // !! this may actually fail. What should we do?
    // const_cast is used to satisfy putenv interface
    using namespace std;
    putenv( const_cast<char*>( fs.str().c_str() ) );
}
#endif
#endif

#ifdef NDNBOOST_MSVC 
#pragma warning(pop) 
#endif 

#define NDNBOOST_RT_PARAM_LITERAL( l ) l
#define NDNBOOST_RT_PARAM_CSTRING_LITERAL( l ) cstring( l, sizeof( l ) - 1 )
#define NDNBOOST_RT_PARAM_GETENV getenv
#define NDNBOOST_RT_PARAM_PUTENV ::ndnboost::NDNBOOST_RT_PARAM_NAMESPACE::putenv_impl
#define NDNBOOST_RT_PARAM_EXCEPTION_INHERIT_STD

//____________________________________________________________________________//

#  else

typedef wchar_t                                                 char_type;
typedef std::basic_string<char_type>                            dstring;
typedef unit_test::basic_cstring<wchar_t const>                 cstring;
typedef const unit_test::basic_cstring<wchar_t const>           literal_cstring;
typedef wrap_wstringstream                                      format_stream;
typedef std::wostream                                           out_stream;

#ifndef UNDER_CE
inline void
putenv_impl( cstring name, cstring value )
{
    format_stream fs;

    fs << name << '=' << value;

    // !! this may actually fail. What should we do?
    // const_cast is used to satisfy putenv interface
    using namespace std;
    wputenv( const_cast<wchar_t*>( fs.str().c_str() ) );
}
#endif

#define NDNBOOST_RT_PARAM_LITERAL( l ) L ## l
#define NDNBOOST_RT_PARAM_CSTRING_LITERAL( l ) cstring( L ## l, sizeof( L ## l )/sizeof(wchar_t) - 1 )
#define NDNBOOST_RT_PARAM_GETENV wgetenv
#define NDNBOOST_RT_PARAM_PUTENV putenv_impl

#  endif
#endif

#ifdef __GNUC__
#define NDNBOOST_RT_PARAM_UNNEEDED_VIRTUAL virtual
#else
#define NDNBOOST_RT_PARAM_UNNEEDED_VIRTUAL
#endif

//____________________________________________________________________________//

} // namespace NDNBOOST_RT_PARAM_NAMESPACE

} // namespace ndnboost

#endif // NDNBOOST_RT_CONFIG_HPP_062604GER
