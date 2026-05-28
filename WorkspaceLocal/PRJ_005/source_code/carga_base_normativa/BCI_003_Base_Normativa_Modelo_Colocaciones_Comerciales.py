# Databricks notebook source
# MAGIC %md
# MAGIC # BCI_003_Base_Normativa_Modelo_Colocaciones_Comerciales

# COMMAND ----------

# MAGIC %md
# MAGIC ## Información del Notebook

# COMMAND ----------

# MAGIC %md
# MAGIC ### Encabezado
# MAGIC **************************************************************************
# MAGIC * Nombre: BCI_003_Base_Normativa_Modelo_Colocaciones_Comerciales.ipynb
# MAGIC * Ruta: 
# MAGIC * Autor:  BitSolucion Spa
# MAGIC * Ing. SW BCI: <alguien.de.ti@bci.cl> - Marcelo Lizama <marcelo.lizama@bci.cl> - Ivan Jara <ivan.jara@bci.cl>
# MAGIC * Fecha: 22/12/2025
# MAGIC * Descripción: Notebook encargado de registrar información de Operaciones Activas - Contingentes desde d00 Segmentado Prima
# MAGIC * Documentación: 
# MAGIC ***************************************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Mantenciones
# MAGIC **************************************************************************
# MAGIC #### Mantención Nro: 
# MAGIC * Autor: <Nombre Autor> (<Empresa del Autor (Bci/Otra)>) - Ing. SW BCI: <Nombre Ing. SW BCI>
# MAGIC * Fecha: <dd/mm/yyyy> 
# MAGIC * Descripción: <Descripción de la mantención>      
# MAGIC ************************************************

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tablas Entrada y Salida
# MAGIC **************************************************************************
# MAGIC #### Tabla Entrada ADLS: 
# MAGIC * tmp_d00_deuda_act_ctg
# MAGIC * dsr_slv_Parametricas_db.tbu
# MAGIC ***************************************************************************
# MAGIC #### Tabla Salida Temporal: 
# MAGIC * tmp_colcomer

# COMMAND ----------

# MAGIC %md
# MAGIC ## Captura de Variables

# COMMAND ----------

# MAGIC %md
# MAGIC ### Crear Widgets para Captura de Variables

# COMMAND ----------

dbutils.widgets.removeAll()
dbutils.widgets.text("fechaProcesoW","","01 Fecha Proceso :")
dbutils.widgets.text("db_plat_tempW","","02 platinum temp db:")
dbutils.widgets.text("db_location_plat_tempW","","03 Location platinum temp db:")
dbutils.widgets.text("db_RiesgoCredW","","04 slv_RiesgoCred_RiesgoCredPer DB:")
dbutils.widgets.text("db_slv_ParametricasW","","05 Parametricas DB:")

# COMMAND ----------

# MAGIC %md
# MAGIC ### Asignar Objeto a Lectura de Widgets y Variables

# COMMAND ----------

fechaProcesoX = dbutils.widgets.get("fechaProcesoW")
spark.conf.set("bci.fechaProcesoX", fechaProcesoX)

db_plat_tempX = dbutils.widgets.get("db_plat_tempW")
spark.conf.set("bci.db_plat_tempX", db_plat_tempX)

db_location_plat_tempX = dbutils.widgets.get("db_location_plat_tempW")
spark.conf.set("bci.db_location_plat_tempX", db_location_plat_tempX)

db_RiesgoCredX = dbutils.widgets.get("db_RiesgoCredW")
spark.conf.set("bci.db_RiesgoCredX", db_RiesgoCredX)

db_slv_ParametricasX = dbutils.widgets.get("db_slv_ParametricasW")
spark.conf.set("bci.db_slv_ParametricasX", db_slv_ParametricasX)

print("fechaProcesoX: " + fechaProcesoX)
print("db_plat_tempX: " + db_plat_tempX)
print("db_location_plat_tempX: " + db_location_plat_tempX)
print("db_RiesgoCredX: " + db_RiesgoCredX)
print("db_slv_ParametricasX: " + db_slv_ParametricasX)

# COMMAND ----------

# MAGIC %md
# MAGIC ### Carga de funciones

# COMMAND ----------

# MAGIC %run "../../Funciones"

# COMMAND ----------

# MAGIC %md
# MAGIC ## Validación de ingreso de parámetros

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Parametro Vacio

# COMMAND ----------

valida_param_vacio(fechaProcesoX,'fechaProcesoX')
#valida_param_vacio(db_platinumX,'db_platinumX')
valida_param_vacio(db_plat_tempX,'db_plat_tempX')
valida_param_vacio(db_RiesgoCredX,'db_RiesgoCredX')
valida_param_vacio(db_location_plat_tempX,'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Base de Datos

# COMMAND ----------

#valida_bd(db_platinumX, 'db_platinumX')
valida_bd(db_plat_tempX, 'db_plat_tempX')
valida_bd(db_RiesgoCredX, 'db_RiesgoCredX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Valida

# COMMAND ----------

valida_fecha_valida(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Fecha Futura

# COMMAND ----------

valida_fecha_futura(fechaProcesoX, 'fechaProcesoX')

# COMMAND ----------

# MAGIC %md
# MAGIC ### Validacion Ruta 

# COMMAND ----------

valida_ruta(db_location_plat_tempX, 'db_location_plat_tempX')

# COMMAND ----------

# MAGIC %md
# MAGIC ## Asigan Variables de fecha

# COMMAND ----------

ano = str(fechaProcesoX)[:4]
mes = str(fechaProcesoX)[4:][:2]
dia = str(fechaProcesoX)[6:][:2]
fechanormativo = str(ano+'-'+mes+'-'+dia)
fechacinta = str(dia+'-'+mes+'-'+ano)
anomes = str(ano+mes)
anomesdia = str(ano+mes+dia)

print("fecha_Formato1: " + ano)
print("fecha_Formato2: " + mes)
print("fecha_Formato3: " + dia)
print("fecha_Formato4: " + fechanormativo)
print("fecha_Formato5: " + fechacinta)
print("fecha_Formato6: " + anomes)
print("fecha_Formato7: " + anomesdia)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Inicio Logica

# COMMAND ----------

# MAGIC %md
# MAGIC ## Creacion tablas temporales

# COMMAND ----------

# MAGIC %md
# MAGIC ### Tabla tmp_colcomer

# COMMAND ----------

paso_tb_del_tmp_colcomer  = """DROP TABLE IF EXISTS """ + db_plat_tempX + """.tmp_colcomer"""

# COMMAND ----------

sql_safe(paso_tb_del_tmp_colcomer)

# COMMAND ----------

dbutils.fs.rm(db_location_plat_tempX+"tmp_colcomer", True)

# COMMAND ----------

paso_tb_crea_tmp_colcomer  = """CREATE TABLE """ + db_plat_tempX + """.tmp_colcomer
(
  periodo_id INT COMMENT 'Periodo de ejecucion SSAAMM',
  rut_cliente INT COMMENT 'RUT del Cliente Titular',
  dv_cliente STRING COMMENT 'Digito verificador del cliente titular',
	cod_ope_original STRING COMMENT 'Operación Original (TRZ)',
  cod_num_operacion STRING COMMENT 'Número Operación',
  cod_tip_cart STRING COMMENT 'Tipo cartera (OPE_VIGENTE) ',
	cod_tipo_ope STRING COMMENT 'Tipo-subtipo del D00',
	des_producto_ope STRING COMMENT  'Descripción Tipo de Cartera',
  des_banca_ope STRING COMMENT 'Descripción operacion banca',
	cod_cartera_ope STRING COMMENT 'Subtipo cartera',
	mto_deuda_ope	   DECIMAL(18,0) COMMENT 'Saldo total ifrs',
  num_dias_mora_ope INT COMMENT 'Dias de mora',
  ind_cdet STRING COMMENT 'Indicador cartera deterioro',
  fec_ingreso_deteriodo_ope DATE COMMENT 'Fecha ingreso de deterioro de la operación',
  cod_cctb STRING COMMENT 'Cuenta contable',
  fl_ope_reneg STRING COMMENT 'Flag de cartera renegociada'
)
USING DELTA
PARTITIONED BY (periodo_id)
LOCATION '"""+ db_location_plat_tempX +"""tmp_colcomer' """

# COMMAND ----------

sql_safe(paso_tb_crea_tmp_colcomer)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Desarrollo Logica

# COMMAND ----------

diaHabil = spark.sql("""select max(fecha_informada) as periodo_id  FROM  """+ db_slv_ParametricasX +""".TBU  
where substring(fecha_informada, 1, 6) = """+ anomes +"""  """)

ultimoDiaHabil_tbu = diaHabil.toPandas()
dia_tbu = ultimoDiaHabil_tbu.iloc[0]["periodo_id"]
 
print(dia_tbu)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 1: Obtencion Colocaciones Comerciales (Tipo Activo 11)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de colocaciones comerciales provenientes desde tabla temporal tmp_d00_deuda_act_ctg generada en Notebook 001.
# MAGIC
# MAGIC - **145400101**: Préstamos en el país (Préstamos comerciales)
# MAGIC - **145400102**: Préstamos en el exterior (Préstamos comerciales)
# MAGIC - **145400201**: Acreditivos negociados a plazo de exportaciones chilenas (Créditos de comercio exterior)
# MAGIC - **145400202**: Otros créditos para exportaciones chilenas (Créditos de comercio exterior)
# MAGIC - **145400203**: Acreditivos negociados a plazo de importaciones chilenas (Créditos de comercio exterior)
# MAGIC - **145400204**: Otros créditos para importaciones chilenas (Créditos de comercio exterior)
# MAGIC - **145400205**: Acreditivos negociados a plazo de operaciones entre terceros países (Créditos de comercio exterior)
# MAGIC - **145400290**: Otros créditos para operaciones entre terceros países (Créditos de comercio exterior)
# MAGIC - **145400300**: Deudores en cuentas corrientes
# MAGIC - **145400401**: Créditos e empresas titulates de TC Visa (Deudores por tarjetas de crédito)
# MAGIC - **145400402**: Utilizaciones de tarjetas de crédito por cobrar (Deudores por tarjetas de crédito)
# MAGIC - **145400901**: Deudores por pago de obligaciones avaladas (Otros créditos y cuentas por cobrar)
# MAGIC - **145400902**: Deudores por boletas de garantía pagadas (Otros créditos y cuentas por cobrar)
# MAGIC - **145400990**: Otras cuentas por cobrar (Otros créditos y cuentas por cobrar)
# MAGIC
# MAGIC **Tabla de salida**: tmp_colcomer(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: Parte numerica de rut cliente
# MAGIC - **dv_cliente** : Dígito verificador de rut cliente
# MAGIC - **cod_ope_original** : Código Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Código Número Operación
# MAGIC - **cod_tip_cart**: Código Tipo cartera 
# MAGIC - **cod_tipo_ope**: Código Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Código Subtipo cartera
# MAGIC - **mto_deuda_ope**:	Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada

# COMMAND ----------

query_ins_tmp_colcomer_1 = """ insert into """+db_plat_tempX+""".tmp_colcomer  
select d00.periodo_id
	,d00.rut_cliente
  ,d00.dv_cliente
	,d00.cod_ope_original
  ,d00.cod_num_operacion
  ,d00.cod_tip_cart 
	,d00.cod_tipo_ope
	,d00.des_producto_ope
  ,d00.des_banca_ope
	,d00.cod_cartera_ope
	,d00.mto_deuda_ope
  ,d00.num_dias_mora_ope
  ,d00.ind_cdet
  ,d00.fec_ingreso_deteriodo_ope
  ,d00.cod_cctb
  ,d00.fl_ope_reneg 
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
	inner join """+db_slv_ParametricasX+""".TBU ccc
		on trim(d00.cod_cctb) = trim(ccc.cta_ctable) 
  	and d00.periodo_id = substring(ccc.fecha_informada, 1, 6)
where d00.periodo_id =  """+anomes+"""
	and trim(d00.cod_cartera_ope) = 'ACT'
	/*and trim(ccc.cta_cmf) IN (	'1302102','1302201','1302202','1302241','1302242','1302281',
							'1302282','1302300','1302101','1302990','1302912','1302911',
							'1302902','1302901') */
		and trim(ccc.cta_cmf) IN ('145400102','145400201','145400202','145400203','145400204','145400205',
							'145400290','145400300','145400101','145400990','145400402','145400401',
							'145400902','145400901') 
		and trim(d00.cod_tipo_ope) <> 'PLC300' 
  	and ccc.fecha_informada = '""" + str(dia_tbu) + """'  """    

# COMMAND ----------

sql_safe(query_ins_tmp_colcomer_1)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 2: Obtencion PARCHE Colocaciones Comerciales que codigo de operacion comienza con J (Tipo Activo 11)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de colocaciones comerciales provenientes desde tabla temporal tmp_d00_deuda_act_ctg generada en Notebook 001. para Cuentas Ifrs de Otros Activos (190002900) cuyas operaciones Comiencen con J
# MAGIC
# MAGIC **Tabla de salida**: tmp_colcomer(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: Parte numerica de rut cliente
# MAGIC - **dv_cliente** : Dígito verificador de rut cliente
# MAGIC - **cod_ope_original** : Código Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Código Número Operación
# MAGIC - **cod_tip_cart**: Código Tipo cartera (OPE_VIGENTE)
# MAGIC - **cod_tipo_ope**: Código Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Código Subtipo cartera
# MAGIC - **mto_deuda_ope**:	Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada

# COMMAND ----------

query_ins_tmp_colcomer_2 = """ insert into """+db_plat_tempX+""".tmp_colcomer  
select d00.periodo_id
	,d00.rut_cliente
  ,d00.dv_cliente
	,d00.cod_ope_original
  ,d00.cod_num_operacion
  ,d00.cod_tip_cart 
	,d00.cod_tipo_ope
	,d00.des_producto_ope
  ,d00.des_banca_ope
	,d00.cod_cartera_ope
	,d00.mto_deuda_ope
  ,d00.num_dias_mora_ope
  ,d00.ind_cdet
  ,d00.fec_ingreso_deteriodo_ope
  ,d00.cod_cctb
  ,d00.fl_ope_reneg 
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
	inner join """+db_slv_ParametricasX+""".TBU ccc
		on trim(d00.cod_cctb) = trim(ccc.cta_ctable) 
    and d00.periodo_id = substring(ccc.fecha_informada, 1, 6)
where d00.periodo_id =  """+anomes+"""
    and trim(ccc.cta_cmf) IN ('190002900') -- Otros Activos 1800990
    and trim(d00.cod_tipo_ope) <> 'PLC300'
		and trim(d00.cod_num_operacion) like 'J%' 
    and ccc.fecha_informada = '""" + str(dia_tbu) + """' """

# COMMAND ----------

sql_safe(query_ins_tmp_colcomer_2)

# COMMAND ----------

# MAGIC %md
# MAGIC ###Paso 3: Obtencion Operaciones Fogape Covid (Tipo Activo 11)

# COMMAND ----------

# MAGIC %md
# MAGIC se crea una vista del universo de Operaciones Fogape Covid provenientes desde tabla temporal tmp_d00_deuda_act_ctg generada en Notebook 001.  
# MAGIC **Tabla de salida**: tmp_colcomer(vista temporal)
# MAGIC
# MAGIC **variables de salida:**
# MAGIC - **periodo_id**: periodo proceso formato SSAAMM
# MAGIC - **rut_cliente**: Parte numerica de rut cliente
# MAGIC - **dv_cliente** : Dígito verificador de rut cliente
# MAGIC - **cod_ope_original** : Código Operación Original (TRZ)
# MAGIC - **cod_num_operacion**: Código Número Operación
# MAGIC - **cod_tip_cart**: Código Tipo cartera (OPE_VIGENTE)
# MAGIC - **cod_tipo_ope**: Código Tipo-subtipo del D00
# MAGIC - **des_producto_ope**: Descripción Tipo de Cartera
# MAGIC - **des_banca_ope**: Descripción operacion banca
# MAGIC - **cod_cartera_ope**: Código Subtipo cartera
# MAGIC - **mto_deuda_ope**:	Saldo total ifrs
# MAGIC - **num_dias_mora_ope**: Dias de mora
# MAGIC - **ind_cdet**: Indicador cartera deterioro
# MAGIC - **fec_ingreso_deteriodo_ope**: Fecha ingreso de deterioro de la operación
# MAGIC - **cod_cctb**: Cuenta contable
# MAGIC - **fl_ope_reneg**: Flag de cartera renegociada

# COMMAND ----------

query_ins_tmp_colcomer_3 = """ insert into """+db_plat_tempX+""".tmp_colcomer  
select d00.periodo_id
	,d00.rut_cliente
  ,d00.dv_cliente
	,d00.cod_ope_original
  ,d00.cod_num_operacion
  ,d00.cod_tip_cart 
	,d00.cod_tipo_ope
	,d00.des_producto_ope
  ,d00.des_banca_ope
	,d00.cod_cartera_ope
	,d00.mto_deuda_ope
  ,d00.num_dias_mora_ope
  ,d00.ind_cdet
  ,d00.fec_ingreso_deteriodo_ope
  ,d00.cod_cctb
  ,d00.fl_ope_reneg 
from """+db_plat_tempX+""".tmp_d00_deuda_act_ctg d00
where d00.periodo_id =  """+anomes+"""    
    and trim(d00.cod_tipo_ope) IN ('COM582','COM583') """

# COMMAND ----------

sql_safe(query_ins_tmp_colcomer_3)

# COMMAND ----------

# MAGIC %md
# MAGIC ## Conteo de registros insertados en tabla de salida

# COMMAND ----------

registros = spark.sql(""" select count(1) as cant_registros from """+db_plat_tempX+""".tmp_colcomer  where periodo_id = """+anomes+""" """)

reg_insertados = registros.toPandas()
cantidad = reg_insertados.iloc[0]["cant_registros"]

print("Se Insertaron " + str(cantidad) +" Registros en la tabla temporal tmp_colcomer ")

# COMMAND ----------

# MAGIC %md
# MAGIC # Mensaje Final

# COMMAND ----------

dbutils.notebook.exit("{\"coderror\":\"0\", \"msgerror\":\"Notebook termina ejecucion satisfactoriamente\"}")