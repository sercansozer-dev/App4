/**
 * @file    GoGdpTransform.h
 * @brief   Declares the GoPxLSdk.GoGdpTransform class.
 * 
 * @internal
 * Copyright (C) 2022-2025 by LMI Technologies Inc.
 * Licensed under the MIT License.
 * Redistributed files must retain the above copyright notice.
 */
#ifndef GO_PXL_SDK_GOGDPTRANSFORM_H
#define GO_PXL_SDK_GOGDPTRANSFORM_H

#include <GoApi/GoApi.h>
#include <kApi/Io/kSerializer.h>
#include <GoPxLSdk/GoGdpMsg/GoGdpMsgDef.h>

class GoGdpMsgTests;

namespace GoPxLSdk
{

    /*
    This class represents a 3D transformation matrix

    [ xx xy xz xt ]
    [ yx yy yz yt ]
    [ zx zy zz zt ]
    [ 0  0  0  1  ]
    */

    class GoPxLSdkClass GoGdpTransform
    {

    friend class ::GoGdpMsgTests;

    public:
        /**
        * Constructs GoGdpTransform.
        *
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        */
        GoGdpTransform() = default;

        /**
        * Deserialize the transform data.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @param serializer      The serializer to read.
        * @throws Go::Exception  If failed to deserialize transform.
        */
        void Deserialize(kSerializer serializer);

        /**
        * Get the XX value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The XX value of transform matrix.
        */
        const k64f XX() const;

        /**
        * Get the XY value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The XY value of transform matrix.
        */
        const k64f XY() const;

        /**
        * Get the XZ value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The XZ value of transform matrix.
        */
        const k64f XZ() const;

        /**
        * Get the XT value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The XT value of transform matrix.
        */
        const k64f XT() const;

        /**
        * Get the YX value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The YX value of transform matrix.
        */
        const k64f YX() const;

        /**
        * Get the YY value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The YY value of transform matrix.
        */
        const k64f YY() const;

        /**
        * Get the YZ value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The YZ value of transform matrix.
        */
        const k64f YZ() const;

        /**
        * Get the YT value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The YT value of transform matrix.
        */
        const k64f YT() const;

        /**
        * Get the ZX value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The ZX value of transform matrix.
        */
        const k64f ZX() const;

        /**
        * Get the ZY value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The ZY value of transform matrix.
        */
        const k64f ZY() const;

        /**
        * Get the ZZ value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The ZZ value of transform matrix.
        */
        const k64f ZZ() const;

        /**
        * Get the ZT value of the 3D transformation matrix.
        * 
        * @public                @memberof GoGdpTransform
        * @version               Introduced in 0.3.5.24.
        * @return                The ZT value of transform matrix.
        */
        const k64f ZT() const;

    private:
        k64f xx = 1.0;
        k64f xy = 0.0;
        k64f xz = 0.0;
        k64f xt = 0.0;

        k64f yx = 0.0;
        k64f yy = 1.0;
        k64f yz = 0.0;
        k64f yt = 0.0;

        k64f zx = 0.0;
        k64f zy = 0.0;
        k64f zz = 1.0;
        k64f zt = 0.0;

    };
}

#endif
