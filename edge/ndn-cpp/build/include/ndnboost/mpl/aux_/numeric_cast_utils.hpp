
#ifndef NDNBOOST_MPL_AUX_NUMERIC_CAST_HPP_INCLUDED
#define NDNBOOST_MPL_AUX_NUMERIC_CAST_HPP_INCLUDED

// Copyright Aleksey Gurtovoy 2003-2004
//
// Distributed under the Boost Software License, Version 1.0. 
// (See accompanying file LICENSE_1_0.txt or copy at 
// http://www.boost.org/LICENSE_1_0.txt)
//
// See http://www.boost.org/libs/mpl for documentation.

// $Id$
// $Date$
// $Revision$

#include <ndnboost/mpl/numeric_cast.hpp>
#include <ndnboost/mpl/apply_wrap.hpp>
#include <ndnboost/mpl/aux_/config/forwarding.hpp>

namespace ndnboost { namespace mpl { namespace aux {

template<
      typename F
    , typename Tag1
    , typename Tag2
    >
struct cast1st_impl
{
    template< typename N1, typename N2 > struct apply
#if !defined(NDNBOOST_MPL_CFG_NO_NESTED_FORWARDING)
        : apply_wrap2< 
              F
            , typename apply_wrap1< NDNBOOST_MPL_AUX_NUMERIC_CAST<Tag1,Tag2>,N1 >::type
            , N2
            >
    {
#else
    {
    typedef typename apply_wrap2< 
              F
            , typename apply_wrap1< NDNBOOST_MPL_AUX_NUMERIC_CAST<Tag1,Tag2>,N1 >::type
            , N2
            >::type type;
#endif
    };
};

template<
      typename F
    , typename Tag1
    , typename Tag2
    >
struct cast2nd_impl
{
    template< typename N1, typename N2 > struct apply
#if !defined(NDNBOOST_MPL_CFG_NO_NESTED_FORWARDING)
        : apply_wrap2< 
              F
            , N1
            , typename apply_wrap1< NDNBOOST_MPL_AUX_NUMERIC_CAST<Tag2,Tag1>,N2 >::type
            >
    {
#else
    {
        typedef typename apply_wrap2< 
              F
            , N1
            , typename apply_wrap1< NDNBOOST_MPL_AUX_NUMERIC_CAST<Tag2,Tag1>,N2 >::type
            >::type type;
#endif
    };
};

}}}

#endif // NDNBOOST_MPL_AUX_NUMERIC_CAST_HPP_INCLUDED